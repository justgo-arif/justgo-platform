using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using static System.Net.Mime.MediaTypeNames;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace JustGo.Authentication.Infrastructure.FileSystemManager.AzureBlob
{
    public class AzureBlobFileService : IAzureBlobFileService, IDisposable
    {
        private BlobContainerClient? _privateClient;
        private BlobContainerClient? _publicClient;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private readonly Dictionary<string, string> ConfigCache = new();
        private bool _isInitialized;
        private bool _disposed;

        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        public AzureBlobFileService(ISystemSettingsService systemSettingsService, IHttpClientFactory httpClientFactory)
        {
            _systemSettingsService = systemSettingsService;
            _httpClientFactory = httpClientFactory;
            _contentTypeProvider = new FileExtensionContentTypeProvider();
        }

        private async Task InitializeClientsSync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized) return;
            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_isInitialized)
                {
                    var storageRoot = await GetCachedSetting("SYSTEM.AZURESTOREROOT", cancellationToken);
                    var storageKey = await GetCachedSetting("SYSTEM.AZURESTOREKEY", cancellationToken);
                    var storageAccount = await GetCachedSetting("SYSTEM.AZURESTOREACCOUNT", cancellationToken);

                    if (string.IsNullOrEmpty(storageRoot) || string.IsNullOrEmpty(storageKey) || string.IsNullOrEmpty(storageAccount))
                    {
                        throw new InvalidOperationException("Azure Blob Storage configuration is not set.");
                    }

                    var credential = new StorageSharedKeyCredential(storageAccount, storageKey);
                    var privateContainerUri = new Uri($"{storageRoot}/001");
                    var publicContainerUri = new Uri($"{storageRoot}/002");

                    _privateClient = new BlobContainerClient(privateContainerUri, credential);
                    _publicClient = new BlobContainerClient(publicContainerUri, credential);
                    _isInitialized = true;
                }
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
        private async Task<string?> GetCachedSetting(string key, CancellationToken cancellationToken = default)
        {
            if (ConfigCache.TryGetValue(key, out var cachedValue))
                return cachedValue;

            var itemKeys = new List<string>()
                {
                    "SYSTEM.AZURESTOREROOT"
                    ,"SYSTEM.AZURESTOREKEY"
                    ,"SYSTEM.AZURESTOREACCOUNT"
                    ,"CLUBPLUS.HOSTSYSTEMID"
                    ,"SYSTEM.SITEADDRESS"
                };
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);

            foreach (var itemKey in itemKeys)
            {
                var value = systemSettings?.Where(w => w.ItemKey == itemKey)?.Select(s => s.Value).SingleOrDefault();
                ConfigCache[itemKey] = value ?? string.Empty;
            }

            return ConfigCache.TryGetValue(key, out var result) ? result : null;
        }
        public async Task<string> MapPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            path = path.Replace("\\\\", "\\").Replace("//", "/").Replace("/", "\\").Replace("\\/", "\\");

            if (path.ToLowerInvariant().StartsWith("~\\store"))
            {
                var storeIndex = path.ToLowerInvariant().IndexOf("store", StringComparison.OrdinalIgnoreCase);
                path = path.Substring(storeIndex + 6);
            }

            var hostSystemId = await GetCachedSetting("CLUBPLUS.HOSTSYSTEMID");
            if (!string.IsNullOrEmpty(hostSystemId) && !path.Contains(hostSystemId))
            {
                path = path.StartsWith("\\") ? $"\\{hostSystemId}{path}" : $"{hostSystemId}\\{path}";
            }

            return path;
        }
#if NET9_0_OR_GREATER
        public async Task<string> UploadFileAsync(string fullPath, IFormFile formFile,
            CancellationToken cancellationToken = default)
        {
            var isPrivate = await IsPrivateContainer(fullPath);
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var blobClient = isPrivate ? _privateClient.GetBlobClient(fullPath) : _publicClient.GetBlobClient(fullPath);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(Path.GetFileName(fullPath))
            };

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Conditions = null // overwrite existing file
            };

            await using var fileStream = formFile.OpenReadStream();
            await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);
            return blobClient.Uri.ToString();
        }
#endif

        public async Task<string> UploadFileAsync(string fullPath, byte[] data, FileMode mode,
            CancellationToken cancellationToken = default)
        {
            using var fileStream = new MemoryStream(data, writable: false);
            if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));

            var isPrivate = await IsPrivateContainer(fullPath);
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var blobClient = isPrivate ? _privateClient.GetBlobClient(fullPath) : _publicClient.GetBlobClient(fullPath);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(Path.GetFileName(fullPath))
            };

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Conditions = null // overwrite existing file
            };

            await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);
            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var mappedPath = await MapPath(filePath);
            var isPrivate = await IsPrivateContainer(filePath);
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var blobClient = isPrivate ? _privateClient.GetBlobClient(mappedPath) : _publicClient.GetBlobClient(mappedPath);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Blob not found: {mappedPath}");
            }

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        public async Task<BlobClient?> GetBolbClientAsync(string fullPath, CancellationToken cancellationToken = default)
        {
            BlobClient? file = null;
            var isPrivate = await IsPrivateContainer(fullPath);
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }

            file = isPrivate ? _privateClient.GetBlobClient(fullPath) : _publicClient.GetBlobClient(fullPath);
            return file;
        }
        public async Task<bool> Exists(string fullPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fullPath)) return false;

            var mappedPath = await MapPath(fullPath);
            var isPrivate = await IsPrivateContainer(fullPath);
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var blobClient = isPrivate ? _privateClient.GetBlobClient(mappedPath) : _publicClient.GetBlobClient(mappedPath);

            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        public async Task<bool> CopyFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sourceFilePath)) throw new ArgumentException("Source file path cannot be null or empty", nameof(sourceFilePath));
            if (string.IsNullOrEmpty(destinationFilePath)) throw new ArgumentException("Destination file path cannot be null or empty", nameof(destinationFilePath));

            var sourceMappedPath = await MapPath(sourceFilePath);
            var destMappedPath = await MapPath(destinationFilePath);

            var sourceIsPrivate = await IsPrivateContainer(sourceFilePath);
            var destIsPrivate = await IsPrivateContainer(destinationFilePath);

            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var sourceBlobClient = sourceIsPrivate ? _privateClient.GetBlobClient(sourceMappedPath) : _publicClient.GetBlobClient(sourceMappedPath);
            var destBlobClient = destIsPrivate ? _privateClient.GetBlobClient(destMappedPath) : _publicClient.GetBlobClient(destMappedPath);

            if (!await sourceBlobClient.ExistsAsync(cancellationToken))
            {
                return false;
            }

            var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);
            await copyOperation.WaitForCompletionAsync(cancellationToken);

            return copyOperation.HasCompleted && copyOperation.GetRawResponse().Status == 202;
        }
        public async Task MoveFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
        {
            var sourceMappedPath = await MapPath(sourceFilePath);
            var destinationMappedPath = await MapPath(destinationFilePath);
            var sourceIsPrivateTask = IsPrivateContainer(sourceFilePath);
            var destIsPrivateTask = IsPrivateContainer(destinationFilePath);

            await Task.WhenAll(sourceIsPrivateTask, destIsPrivateTask);

            var sourceIsPrivate = await sourceIsPrivateTask;
            var destIsPrivate = await destIsPrivateTask;
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var sourceBlob = sourceIsPrivate
            ? _privateClient.GetBlobClient(await MapPath(sourceMappedPath))
            : _publicClient.GetBlobClient(await MapPath(sourceMappedPath));

            var destinationBlob = destIsPrivate
                ? _privateClient.GetBlobClient(await MapPath(destinationMappedPath))
                : _publicClient.GetBlobClient(await MapPath(destinationMappedPath));

            var sourceExists = await sourceBlob.ExistsAsync(cancellationToken);
            if (!sourceExists.Value)
            {
                return;
            }

            var copyOperation = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: cancellationToken);

            await copyOperation.WaitForCompletionAsync(cancellationToken);
            if (copyOperation.HasCompleted /*&& copyOperation.GetRawResponse().Status == 202*/)
            {
                await sourceBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            }
            //else
            //{
            //    await destinationBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            //}        
        }
        public async Task<bool> MoveToAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
        {
            var copySuccessful = await CopyFileAsync(sourceFilePath, destinationFilePath, cancellationToken);
            if (copySuccessful)
            {
                await DeleteFileAsync(sourceFilePath, cancellationToken);
                return true;
            }
            return false;
        }
        public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var mappedPath = await MapPath(filePath);
            var isPrivate = await IsPrivateContainer(filePath);
            await InitializeClientsSync(cancellationToken);
            if (_privateClient is null || _publicClient is null)
            {
                throw new InvalidOperationException("Blob container clients are not initialized.");
            }
            var blobClient = isPrivate ? _privateClient.GetBlobClient(mappedPath) : _publicClient.GetBlobClient(mappedPath);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        }
        private string GetContentType(string fileName)
        {
            // Add any custom types your application needs
            //_contentTypeProvider.Mappings[".dwg"] = "image/vnd.dwg";
            // Remove unwanted mappings
            //provider.Mappings.Remove(".exe");
            if (_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                return contentType;

            return "application/octet-stream";
        }

        private async Task<bool> IsPrivateContainer(string path)
        {
            var mappedPath = await MapPath(path);
            var parts = mappedPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2) return false;

            return parts[1].ToLowerInvariant() switch
            {
                "repository" => parts.Length > 5,
                "correspondentattachments" or "froalaattachments" or "fieldmanagementattachment" or "assetattachment" or "assetleaseattachment" => true,
                "justgobookingattachment" or "competitionattachment" => false,
                "temp" => !mappedPath.Contains("justgobookingattachment", StringComparison.OrdinalIgnoreCase) && !mappedPath.Contains("competitionattachment", StringComparison.OrdinalIgnoreCase) &&
                         (mappedPath.Contains("attachment", StringComparison.OrdinalIgnoreCase) ||
                          mappedPath.Contains("default\\", StringComparison.OrdinalIgnoreCase) ||
                          mappedPath.Contains("DataImport", StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }
        public async Task<string> GetImageUrlAsync(string relativeUrl, string? gender, CancellationToken cancellationToken)
        {
            string imageUrl;
            var client = _httpClientFactory.CreateClient("AzurePublicApiClient");
            var storageRoot = await GetCachedSetting("SYSTEM.AZURESTOREROOT", cancellationToken);
            var hostMid = await GetCachedSetting("CLUBPLUS.HOSTSYSTEMID", cancellationToken);
            var baseUrl = $"{storageRoot}/002/{hostMid}";
            var fullUrl = $"{baseUrl}/{relativeUrl}";
            using var request = new HttpRequestMessage(HttpMethod.Head, fullUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                imageUrl = fullUrl;
            }
            else
            {
                imageUrl = await GetDefaultAvatarUrl(gender, cancellationToken);
            }
            return imageUrl;
        }
        public async Task<string> GetDefaultAvatarUrl(string? gender, CancellationToken cancellationToken)
        {
            var siteUrl = await GetCachedSetting("SYSTEM.SITEADDRESS", cancellationToken);
            if (string.IsNullOrWhiteSpace(siteUrl))
                return string.Empty;

            var baseUrl = siteUrl.TrimEnd('/');
            var genderCode = string.IsNullOrWhiteSpace(gender) ? "M" :
                            gender.Trim().ToUpperInvariant().Substring(0, 1);

            return $"{baseUrl}/Media/Images/avatar-{genderCode}.png";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _initializationSemaphore?.Dispose();
                _disposed = true;
            }
        }

        public async Task<string> CreateThumbnailAsync(string sourcePath, string targetDir = "", int width = 50, int height = 50, CancellationToken cancellationToken = default)
        {
            using var srcStream = await DownloadFileAsync(sourcePath, cancellationToken);
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(srcStream, cancellationToken);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            string thumbName = "Thumb.png";
            string finalTargetDir = string.IsNullOrEmpty(targetDir)
                ? Path.GetDirectoryName(sourcePath)?.Replace("\\", "/") ?? ""
                : targetDir.Replace("\\", "/");
            string thumbPath = $"{finalTargetDir}/{thumbName}".Replace("//", "/");

            using var thumbStream = new MemoryStream();
            await image.SaveAsync(thumbStream, new PngEncoder(), cancellationToken);
            thumbStream.Position = 0;

            var thumbBytes = thumbStream.ToArray();
            await UploadFileAsync(thumbPath, thumbBytes, FileMode.Create, cancellationToken);

            return thumbPath;
        }
    }
}
#endif
