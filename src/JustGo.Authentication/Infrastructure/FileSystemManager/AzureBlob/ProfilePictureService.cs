using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
#if NET9_0_OR_GREATER
namespace JustGo.Authentication.Infrastructure.FileSystemManager.AzureBlob
{
    public class ProfilePictureService : IProfilePictureService
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly ISystemSettingsService _systemSettingsService;   
        private readonly IAzureBlobFileService _azureBlobFileService;
        private static readonly Dictionary<string, string> _configCache = new();
        public ProfilePictureService(IReadRepositoryFactory readRepository
            ,ISystemSettingsService systemSettingsService
            , IAzureBlobFileService azureBlobFileService)
        {
            _readRepository = readRepository;
            _systemSettingsService = systemSettingsService;
            _azureBlobFileService = azureBlobFileService;
        }

        public async Task<string> GetProfilePictureUrlAsync(Guid id, CancellationToken cancellationToken)
        {            
            var imageUrl = string.Empty;
            string sql = @"SELECT [Userid]
                              ,[ProfilePicURL]
                              ,[Gender]
                          FROM [dbo].[User]
                          WHERE [UserSyncId]=@UserSyncId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", id);
            var member = await _readRepository.GetLazyRepository<dynamic>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            
            if (member is not null && member?.Userid is not null && !string.IsNullOrWhiteSpace(member?.ProfilePicURL))
            {
                imageUrl = $"User/{member?.Userid}/{member?.ProfilePicURL}";
                return await _azureBlobFileService.GetImageUrlAsync(imageUrl, member?.Gender, cancellationToken);
            }
            if (member is not null)
            {
                return await _azureBlobFileService.GetDefaultAvatarUrl(member.Gender, cancellationToken);
            }                
            return imageUrl;
        }


        
    }
}
#endif
