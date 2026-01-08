using Google.Apis.Auth.OAuth2;
using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using CloudStoragePlatform.Core.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;
using CloudStoragePlatform.Core.Domain.IdentityEntites;
using CloudStoragePlatform.Core.Domain.RepositoryContracts;
using CloudStoragePlatform.Core.DTO;
using CloudStoragePlatform.Core.Domain.Entities;
using File = CloudStoragePlatform.Core.Domain.Entities.File;

namespace CloudStoragePlatform.Core.Services
{
    public class AiUpscaleProcessor : IAiUpscaleProcessor
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _provider;
        private readonly UserIdentification userIdentification;
        private readonly IFilesRetrievalService _filesRetrievalService;

        public AiUpscaleProcessor(IConfiguration config, IServiceProvider serviceProvider, UserIdentification userIdentification, IFilesRetrievalService filesRetrievalService)
        {
            _config = config;
            _provider = serviceProvider;
            this.userIdentification = userIdentification;
            _filesRetrievalService = filesRetrievalService;
        }

        public async Task UpscaleDefault(Guid id)
        {
            // Get file using retrieval service
            var fileResponse = await _filesRetrievalService.GetFileByFileId(id);
            if (fileResponse == null)
            {
                throw new ArgumentException("File not found");
            }

            // Get physical storage path from user identification
            string physicalStoragePath = userIdentification.PhysicalStoragePath;
            
            // Concatenate physical storage path and file id to get finalPath
            string finalPath = Path.Combine(physicalStoragePath, id.ToString());

            // TODO move these details to config
            string projectID = "cloud-storage-platform-rushaan";
            string region = "us-central1";
            string publisher = "google";
            string model = "imagen-4.0-upscale-preview";

            ApplicationUser usr = userIdentification.User!;

            Func<Task> upscaleJob = async () => 
            {
                using var scope = _provider.CreateScope();
                IFilesModificationService fms = scope.ServiceProvider.GetRequiredService<IFilesModificationService>();
                IFilesRepository filesRepository = scope.ServiceProvider.GetRequiredService<IFilesRepository>();
                UserIdentification usrIdentification = scope.ServiceProvider.GetRequiredService<UserIdentification>();
                usrIdentification.User = usr;

                // Get file entity to access ParentFolder
                File? fileEntity = await filesRepository.GetFileByFileId(id);
                Folder? parent = fileEntity?.ParentFolder;
                if (fileEntity == null || parent == null)
                {
                    throw new ArgumentException("File or parent folder not found");
                }

                // Read original file and upscale
                byte[] originalBytes = await System.IO.File.ReadAllBytesAsync(finalPath);
                string b64Original = Convert.ToBase64String(originalBytes);
                byte[] upscaledBytes = await UpscaleImageAsyncInternal(b64Original, projectID, region, publisher, model);

                // Create new file with "Upscaled " prefix
                string upscaledFileName = "UHQ " + fileEntity.FileName;
                upscaledFileName = Utilities.FindUniqueName(parent.Files.Select(f=>f.FileName).ToArray(), upscaledFileName, true);
                string parentFolderPath = parent.FolderPath;
                string upscaledFilePath = Path.Combine(parentFolderPath, upscaledFileName);

                // Create FileAddRequest
                var fileAddRequest = new FileAddRequest
                {
                    FileName = upscaledFileName,
                    FilePath = upscaledFilePath
                };

                // Upload the upscaled file
                using var upscaledStream = new MemoryStream(upscaledBytes);
                await fms.UploadFile(fileAddRequest, upscaledStream);
            };

            _= Task.Run(async () =>
            {
                await MinRamThreshold.WaitForRamOrTimeoutAsync(upscaleJob);
            });

            // TODO most likely handle API ops in this fn
            // TODO since most AI features will need ram optimisation and background jobs, wise to have a singleton background orchestrator service for the same instead of repeating background & scope logic
        }


        public async Task UpscaleImageAsync(string b64, string projectId, string location, string publisher, string modelName)
        {
            await UpscaleImageAsyncInternal(b64, projectId, location, publisher, modelName);
        }

        private async Task<byte[]> UpscaleImageAsyncInternal(string b64, string projectId, string location, string publisher, string modelName)
        {
            string base64 = _config["GoogleServiceAccountJsonKey"];
            string saJson = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

            using var doc = JsonDocument.Parse(saJson);
            var root = doc.RootElement;

            string clientEmail = root.GetProperty("client_email").GetString();
            string privateKeyPem = root.GetProperty("private_key").GetString();

            // 1. Creating RSA key from PEM
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem.ToCharArray()); // ImportFromPem accepts PEM including -----BEGIN PRIVATE KEY-----...


            // 2. Build ServiceAccountCredential initializer
            var initializer = new ServiceAccountCredential.Initializer(clientEmail)
            {
                Scopes = new[] { "https://www.googleapis.com/auth/cloud-platform" }
            };
            initializer.Key = rsa; // set RSA key to sign JWT


            // 3. Create credential and request access token
            var svcCred = new ServiceAccountCredential(initializer);
            bool success = await svcCred.RequestAccessTokenAsync(CancellationToken.None);
            if (!success) throw new Exception("Failed to get access token");

            string accessToken = svcCred.Token.AccessToken;
            Console.WriteLine("Access token length: " + accessToken.Length);



            // real upscale being done below:
            var endpoint = $"{location}-aiplatform.googleapis.com";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            string url = $"https://{endpoint}/v1/projects/{projectId}/locations/{location}/publishers/{publisher}/models/{modelName}:predict";

            string request = BuildRequestJson(b64);
            var content = new StringContent(request, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            string b64_upscaled = json.RootElement
                .GetProperty("predictions")[0]
                .GetProperty("bytesBase64Encoded")
                .GetString();
            
            // Return upscaled bytes instead of writing to file
            return Convert.FromBase64String(b64_upscaled);
        }

        private string BuildRequestJson(string b64, int count = 1, string factor = "x2") 
        {
            return "{\r\n  \"instances\": [\r\n    {\r\n      \"prompt\": \"\",\r\n      \"image\": {\r\n        \"bytesBase64Encoded\": \"" + b64+"\"\r\n      }\r\n    }\r\n  ],\r\n  \"parameters\": {\r\n    \"sampleCount\": "+count+",\r\n    \"mode\": \"upscale\",\r\n    \"upscaleConfig\": {\r\n      \"upscaleFactor\": \""+factor+"\"\r\n    }\r\n  }\r\n}\r\n";
        }
    }
}
