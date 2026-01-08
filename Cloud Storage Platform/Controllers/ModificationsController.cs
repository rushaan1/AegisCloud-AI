using Cloud_Storage_Platform.CustomModelBinders;
using Cloud_Storage_Platform.Filters;
using CloudStoragePlatform.Core;
using CloudStoragePlatform.Core.DTO;
using CloudStoragePlatform.Core.ServiceContracts;
using CloudStoragePlatform.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace Cloud_Storage_Platform.Controllers
{
    [ServiceFilter(typeof(IdentifyUser))]
    [Route("api/[controller]")]
    [ApiController]
    public class ModificationsController : ControllerBase
    {
        private readonly IFoldersModificationService _foldersModificationService;
        private readonly IConfiguration _configuration;
        private readonly IFilesModificationService _filesModificationService;
        private readonly SSE _sse;
        private readonly UserIdentification _userIdentification;
        private readonly IAiUpscaleProcessor _aiUpscaleProcessor;

        public ModificationsController(IFoldersModificationService foldersModificationService, IConfiguration configuration, SSE sse, IFilesModificationService filesModificationService, UserIdentification userIdentification, IAiUpscaleProcessor aiUpscaleProcessor)
        {
            _foldersModificationService = foldersModificationService;
            _configuration = configuration;
            _sse = sse;
            _filesModificationService = filesModificationService;
            _userIdentification = userIdentification;
            _aiUpscaleProcessor = aiUpscaleProcessor;
        }

        [HttpPost]
        [Route("add")]
        public async Task<ActionResult<FolderResponse>> Add(FolderAddRequest folderAddRequest)
        {
            folderAddRequest.FolderPath = _configuration["InitialPathForStorage"] + Uri.UnescapeDataString(folderAddRequest.FolderPath);
            FolderResponse folderResponse = await _foldersModificationService.AddFolder(folderAddRequest);
            var res = new List<FolderResponse>() { folderResponse };
            await _sse.SendEventAsync("added", new { res }, _userIdentification.User.Id);
            return folderResponse;
        }

        private string GetBoundary(MediaTypeHeaderValue mediaTypeHeaderValue)
        {
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeaderValue.Boundary).Value;
            return boundary;
        }

        [HttpPost]
        [Route("upload")]
        [DisableFormValueModelBinding]
        public async Task<ActionResult<List<FileResponse>>> UploadFiles()
        {
            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType));
            var multipartReader = new MultipartReader(boundary, Request.Body);
            var section = await multipartReader.ReadNextSectionAsync();

            var fileRequests = new List<FileAddRequest>(); // Store multiple FileAddRequest objects

            FileAddRequest? currentFileRequest = null;
            int uploadIndex = 0;
            var res = new List<FileResponse>();

            while (section is not null)
            {
                var contentDisposition = section.ContentDisposition;

                if (ContentDispositionHeaderValue.TryParse(contentDisposition, out var disposition))
                {
                    if (disposition.IsFormDisposition()) // This is a form field
                    {
                        using (var reader = new StreamReader(section.Body))
                        {
                            var value = await reader.ReadToEndAsync();

                            // If a new FileAddRequest needs to be created (new file metadata incoming)
                            if (disposition.Name == "fileName")
                            {
                                currentFileRequest = new FileAddRequest
                                {
                                    FileName = Uri.UnescapeDataString(value)
                                };
                                fileRequests.Add(currentFileRequest); // Add to list
                            }
                            else if (disposition.Name == "filePath" && currentFileRequest is not null)
                            {
                                currentFileRequest.FilePath = _configuration["InitialPathForStorage"] + Uri.UnescapeDataString(value);
                            }
                        }
                    }
                    else if (disposition.IsFileDisposition() && currentFileRequest is not null) // This is the actual file
                    {
                        var fileSection = section.AsFileSection();
                        if (fileSection is not null && fileSection.FileStream is not null)
                        {
                            res.Add(await _filesModificationService.UploadFile(fileRequests.ElementAt(uploadIndex), fileSection.FileStream));
                        }
                        uploadIndex++;
                    }
                }

                section = await multipartReader.ReadNextSectionAsync();
            }
            await _sse.SendEventAsync("added", new { res }, _userIdentification.User.Id);
            return res;
        }

        [HttpPatch]
        [Route("rename")]
        public async Task<ActionResult> Rename(RenameRequest renameReq, bool isFolder)
        {
            string newName;
            if (isFolder)
            {
                var response = await _foldersModificationService.RenameFolder(renameReq);
                newName = response.FolderName;
            }
            else
            {
                var response = await _filesModificationService.RenameFile(renameReq);
                newName = response.FileName;
            }
            await _sse.SendEventAsync("renamed", new { id = renameReq.id, val = newName }, _userIdentification.User.Id);
            return NoContent();
        }

        [HttpPatch]
        [Route("addOrRemoveFromFavorite")]
        public async Task<ActionResult> AddOrRemoveFromFavorite(Guid id, bool isFolder)
        {
            if (isFolder)
            {
                var folderResponse = await _foldersModificationService.AddOrRemoveFavorite(id);
                await _sse.SendEventAsync("favorite_updated", new { id = folderResponse.FolderId, res = folderResponse }, _userIdentification.User.Id);
            }
            else
            {
                var fileResponse = await _filesModificationService.AddOrRemoveFavorite(id);
                await _sse.SendEventAsync("favorite_updated", new { id = fileResponse.FileId, res = fileResponse }, _userIdentification.User.Id);
            }
            return NoContent();
        }

        [HttpPatch]
        [Route("batchAddOrRemoveFromTrash")]
        public async Task<ActionResult> BatchAddOrRemoveFromTrash(List<Guid> ids, bool isFolder)
        {
            List<FolderResponse> updatedFolders = new();
            List<FileResponse> updatedFiles = new();
            if (isFolder)
            {
                updatedFolders.Capacity = ids.Count;
            }
            else 
            {
                updatedFiles.Capacity = ids.Count;
            }
            foreach (Guid id in ids)
            {
                if (isFolder)
                {
                    var response = await _foldersModificationService.AddOrRemoveTrash(id);
                    updatedFolders.Add(response);
                }
                else
                {
                    var response = await _filesModificationService.AddOrRemoveTrash(id);
                    updatedFiles.Add(response);
                }
            }
            await _sse.SendEventAsync("trash_updated", new { updatedFolders, updatedFiles }, _userIdentification.User.Id);
            return NoContent();
        }

        [HttpPost]
        [Route("batchFoldersAdd")]
        public async Task<ActionResult> BatchAddFolders(List<string> paths)
        {
            List<FolderResponse> res = new();
            foreach (string path in paths)
            {
                string fullpath = _configuration["InitialPathForStorage"] + Uri.UnescapeDataString(path);
                string[] splittedPath = fullpath.Split("\\");
                res.Add(await _foldersModificationService.AddFolder(new FolderAddRequest()
                {
                    FolderName = splittedPath[splittedPath.Length - 1],
                    FolderPath = fullpath
                }));
            }
            await _sse.SendEventAsync("added", new { res }, _userIdentification.User.Id);
            return NoContent();
        }

        [HttpDelete]
        [Route("batchDelete")]
        public async Task<ActionResult> BatchDelete([FromQuery] List<Guid> ids, bool isFolder)
        {
            int deleted = 0;
            foreach (Guid id in ids)
            {
                if (isFolder)
                {
                    if (await _foldersModificationService.DeleteFolder(id))
                    {
                        deleted++;
                    }
                }
                else
                {
                    if (await _filesModificationService.DeleteFile(id))
                    {
                        deleted++;
                    }
                }
            }
            await _sse.SendEventAsync("deleted", new { ids }, _userIdentification.User.Id);
            return (deleted == ids.Count) ? NoContent() : StatusCode(500);
        }

        [HttpPatch]
        [Route("batchMove")]
        public async Task<ActionResult> BatchMove(List<Guid> ids, [ModelBinder(typeof(AppendToPath))] string newFolderPath, bool isFolder)
        {
            if (ids.Count == 0) 
            {
                return NoContent();
            }
            var movedList = new List<object>();
            foreach (Guid id in ids)
            {
                if (isFolder)
                {
                    var response = await _foldersModificationService.MoveFolder(id, newFolderPath);
                    movedList.Add(new
                    {
                        movedTo = Utilities.ReplaceLastOccurance(response.FolderPath!, "\\"+response.FolderName, ""),
                        id = response.FolderId,
                        res = response
                    });
                }
                else
                {
                    var response = await _filesModificationService.MoveFile(id, newFolderPath);
                    movedList.Add(new
                    {
                        movedTo = Utilities.ReplaceLastOccurance(response.FilePath!, "\\" + response.FileName, ""),
                        id = response.FileId,
                        res = response
                    });
                }
            }
            await _sse.SendEventAsync("moved", movedList, _userIdentification.User.Id);
            return NoContent();
        }

        [HttpPost]
        [Route("UpscaleTest")]
        [AllowAnonymous]
        public async Task<IActionResult> UpscaleTest(Guid id)
        {
            await _aiUpscaleProcessor.UpscaleDefault(id);
            return Ok();
        }

        [HttpGet("sseauth")]
        public async Task<IActionResult> SSEAuth() 
        {
            var token = Guid.NewGuid().ToString();
            SseTokenStore.Save(token, User.FindFirstValue(ClaimTypes.NameIdentifier), TimeSpan.FromMinutes(1));

            return Ok(new { sseToken = token });
        }

        // Instead of directly establishing SSE connection a http req for auth above is needed because firefox browser does not support sending cookies along with sse request so auth cannot be done automatically

        [HttpGet("sse")]
        [AllowAnonymous]
        public async Task ServerSentEvents([FromQuery] string token)
        {
            var userIdStr = SseTokenStore.GetAndRemove(token);
            if (userIdStr == null)
            {
                Response.StatusCode = 403;
                return;
            }

            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");

            _sse.AddClient(Response, new Guid(userIdStr));

            try
            {
                await Task.Delay(-1);
            }
            finally
            {
                _sse.RemoveClient(Response);
            }
        }
    }
}
