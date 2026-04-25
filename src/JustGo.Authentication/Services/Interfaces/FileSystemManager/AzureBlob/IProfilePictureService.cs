using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob
{
    public interface IProfilePictureService
    {
        Task<string> GetProfilePictureUrlAsync(Guid id, CancellationToken cancellationToken);
    }
}
