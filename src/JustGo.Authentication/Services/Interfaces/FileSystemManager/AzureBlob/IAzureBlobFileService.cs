using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob
{
    public interface IAzureBlobFileService
    {
        Task<string> MapPath(string path);
        Task<string> UploadFileAsync(string fullPath, byte[] data, FileMode mode,
            CancellationToken cancellationToken = default);
#if NET9_0_OR_GREATER
        Task<string> UploadFileAsync(string fullPath, IFormFile formFile,
            CancellationToken cancellationToken = default);
#endif
        Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<BlobClient?> GetBolbClientAsync(string fullPath, CancellationToken cancellationToken = default);
        Task<bool> Exists(string fullPath, CancellationToken cancellationToken = default);
        Task<bool> CopyFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default);
        Task MoveFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default);
        Task<bool> MoveToAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<string> GetImageUrlAsync(string relativeUrl, string? gender, CancellationToken cancellationToken);
        Task<string> GetDefaultAvatarUrl(string? gender, CancellationToken cancellationToken);
        Task<string> CreateThumbnailAsync(string sourcePath, string targetDir = "", int width = 50, int height = 50, CancellationToken cancellationToken = default);
    }
}
