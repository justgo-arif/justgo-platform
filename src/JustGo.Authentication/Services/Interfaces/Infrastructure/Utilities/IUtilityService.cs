using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Utilities;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#else
    using System.Web;
#endif

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities
{
    public interface IUtilityService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        string Encrypt(string stringToEncrypt);
        string EncryptData(string strData);
        string DecryptData(string strData);

        string EncryptData2(string plainText);
        string DecryptData2(string encryptedData);
#if NET9_0_OR_GREATER
        Task<int> GetCurrentUserId(CancellationToken cancellationToken);
        Task<CurrentUser> GetCurrentUser(CancellationToken cancellationToken);
        Task<CurrentUser?> GetCurrentUserPublic(CancellationToken cancellationToken);
        Guid GetCurrentUserGuid();
        Task<string?> GetCurrentTenantGuid(CancellationToken cancellationToken);
        Task<string?> GetTenantClientIdByDomain(CancellationToken cancellationToken);
        Task<int?> GetUserIdByMemberIdAsync(string memberId, CancellationToken cancellationToken);
        Task<int?> GetUserIdByMemberDocIdAsync(string memberDocId, CancellationToken cancellationToken);
        string GetDeviceType(IHttpContextAccessor httpContextAccessor);
        (string Browser, string Version) GetBrowserInfo(IHttpContextAccessor httpContextAccessor);
        ClientConnectionInfo GetClientConnectionInfo(IHttpContextAccessor httpContextAccessor);
        string GetClientIpAddress(IHttpContextAccessor httpContextAccessor);
        string GetClientPort(IHttpContextAccessor httpContextAccessor);
        Task<int> GetOwnerIdAsync(CancellationToken cancellationToken);
        Task<int> GetOwnerIdByGuid(string ownerGuid, CancellationToken cancellationToken);

        Task<int> GetUserIdByUserSyncGuidAsync(string userSyncGuid, CancellationToken cancellationToken);

        Task<bool> VerifyOwnerById(int ownerId, CancellationToken cancellationToken);
        Task<List<Group>> SelectGroupByUserAsync(int userID, CancellationToken cancellationToken);
        void SetCookie(string key, string value, double ExpiryMinutes);
        Task<int> GetTenantSportTypeAsync(CancellationToken cancellationToken);
        Task<int?> GetEventTypeIdByGuid(string recordGuid, CancellationToken cancellationToken);
#else
        (string Browser, string Version) GetBrowserInfo(HttpRequest request);
        ClientConnectionInfo GetClientConnectionInfo(HttpRequest request);
        string GetClientIpAddress(HttpRequest request);
        string GetClientPort(HttpRequest request);
        string GetDeviceType(HttpRequest request);
#endif
        string? GetCurrentTenantClientId();

    }
}
