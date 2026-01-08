using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStoragePlatform.Core.ServiceContracts
{
    public interface IAiUpscaleProcessor
    {
        Task UpscaleImageAsync(string b64, string projectId, string location, string publisher, string modelName);
        Task UpscaleDefault(Guid id);
    }
}
