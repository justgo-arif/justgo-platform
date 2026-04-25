using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading; 
using System.Threading.Tasks;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.IdentityModel.Tokens.Jwt;
using Dapper;
#if NET9_0_OR_GREATER
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
#else
using System.Web;
#endif
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.JsonWebTokens;


namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
    public class JwtAthenticationService: IJwtAthenticationService
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly LazyService<IWriteRepository<dynamic>> _writeRepository;
#if NET9_0_OR_GREATER
        private readonly IHttpContextAccessor _httpContextAccessor;
#endif
        private readonly IUtilityService _utilityService;
        private readonly IJweTokenService _jweTokenService;

        private const string userClaimSql = @"
                           SELECT * FROM [dbo].[User] WHERE [LoginId]=@LoginId;
                           SELECT DISTINCT r.[Name] FROM [dbo].[Role] r
                           	  INNER JOIN [dbo].[RoleMembers] rm ON r.RoleId=rm.RoleId
                           	  INNER JOIN [dbo].[User] u ON u.Userid=rm.UserId
                             WHERE u.LoginId=@LoginId;
                           SELECT DISTINCT r.[Id],r.[Name],r.[Description],r.[Status]
                            FROM [dbo].[AbacRoles] r
	                            INNER JOIN [dbo].[AbacUserRoles] ur ON r.[Id]=ur.[RoleId]
	                            INNER JOIN [dbo].[User] u ON u.Userid=ur.UserId
                            WHERE u.[LoginId]=@LoginId;
                           SELECT d.SyncGuid FROM [dbo].[Document] d
                           	INNER JOIN [dbo].[Hierarchies] h ON h.EntityId=d.DocId
                           	INNER JOIN [dbo].[HierarchyLinks] hl ON hl.HierarchyId=h.Id
                           	INNER JOIN [User] u ON u.Userid = hl.UserId
                           WHERE d.RepositoryId=2
                              AND u.LoginId=@LoginId;
                           DECLARE @LookUpIDClubRole INT 
                            DECLARE @LookupFieldIdIsAdmin INT 
                            SELECT @LookUpIDClubRole = LookUpID FROM LookUp WHERE NAME = 'Club Role'
                            SELECT  @LookupFieldIdIsAdmin = LookupFieldId  FROM LookUpFields WHERE LookUpID = @LookUpIDClubRole AND Name = 'IsAdmin'
                            DECLARE @SQL NVARCHAR(MAX)
                            SET @SQL = N'
                            SELECT DISTINCT d.SyncGuid
                             FROM [dbo].[Document] d
                             INNER JOIN [dbo].[ClubMemberRoles] cmr ON cmr.ClubDocId = d.DocId
                             INNER JOIN [dbo].[Lookup_'+cast(@LookUpIDClubRole as varchar)+'] l22 ON l22.RowId = cmr.RoleId
                             INNER JOIN [User] u ON u.Userid = cmr.UserId
                             WHERE d.RepositoryId = 2
                               AND l22.FIELD_'+cast(@LookupFieldIdIsAdmin as nvarchar)+' = ''Yes''
                               AND u.LoginId = @LoginId
                             UNION
                             SELECT d.SyncGuid
                             FROM [dbo].[Document] d
		                            INNER JOIN [dbo].[AbacUserRoles] ur ON ur.[OrganizationId] = d.DocId
		                            INNER JOIN [User] u ON u.Userid = ur.UserId
                             WHERE u.LoginId = @LoginId'
                            EXEC sp_executesql @SQL, N'@LoginId NVARCHAR(100)', @LoginId = @LoginId;
                            SELECT DISTINCT u.UserSyncId
                            FROM [dbo].[UserFamilies] uf
                                INNER JOIN [dbo].[User] u ON uf.UserId = u.Userid
                            WHERE EXISTS (
                                SELECT 1
                                FROM [dbo].[UserFamilies] uf2
                                    INNER JOIN [dbo].[User] u2 ON u2.Userid = uf2.UserId
                                WHERE uf2.FamilyId = uf.FamilyId
                                    AND uf2.IsAdmin = 1
                                    AND u2.LoginId = @LoginId
                            );                           
                           ";
#if NET9_0_OR_GREATER
        public JwtAthenticationService(IReadRepositoryFactory readRepository
            , LazyService<IWriteRepository<dynamic>> writeRepository, IHttpContextAccessor httpContextAccessor, IUtilityService utilityService, IJweTokenService jweTokenService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _httpContextAccessor = httpContextAccessor;
            _utilityService = utilityService;
            _jweTokenService = jweTokenService;
        }
#else
        public JwtAthenticationService(IReadRepositoryFactory readRepository
            , LazyService<IWriteRepository<dynamic>> writeRepository, IUtilityService utilityService, IJweTokenService jweTokenService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
            _jweTokenService = jweTokenService;
        }
#endif
        public string GenerateJwtToken(string tenantClientId, string loginId)
        {
            var tenant = GetTenantByTenantClientId(tenantClientId);
            if (tenant is null)
            {
                throw new Exception("Tenant not found");
            }
            var userClaimsData = GetUserClaimsDataAsync(loginId);
            if (userClaimsData.User is null)
            {
                throw new Exception("Invalid user");
            }
            //var user = GetUserByLoginId(loginId);
            var user = userClaimsData.User;
            if (user is null)
            {
                throw new Exception("Invalid user");
            }
            //var groups = GetGroupsByUserId(user.Userid);
            //if (groups is null)
            //{
            //    throw new Exception("User has no group");
            //}
            //var roles = GetRolesByUser(user.LoginId);
            var roles = userClaimsData.Roles;
            if (roles is null)
            {
                throw new Exception("User has no role");
            }
            //var metaRoles = GetMetaRolesByUser(user.Userid);
            //if (roles is null)
            //{
            //    throw new Exception("User has no meta role");
            //}
            //var abacRoles = GetAbacRolesByUser(user.Userid);
            var abacRoles = userClaimsData.AbacRoles;
            if (abacRoles is null)
            {
                throw new Exception("User has no Abac role");
            }
            //var clubsIn = GetClubsByUserId(user.Userid);
            var clubsIn = userClaimsData.ClubsIn;
            //var clubsAdminOf = GetClubsAdminByUserId(user.Userid);
            var clubsAdminOf = userClaimsData.ClubsAdminOf;
            //var memberships = GetMembershipByUserId(user.Userid);
            //var familyMembers = GetFamilyMembersByUserId(user.Userid);
            var familyMembers = userClaimsData.FamilyMembers;

            var tokenParameter = new JwtTokenParameter();
            tokenParameter.SecretKey = tenant.JwtAccessTokenSecretKey;
            tokenParameter.EncryptionKey = tenant.JwtAccessTokenEncryptionKey ?? tenant.JwtAccessTokenSecretKey; // Fallback to secret key
            tokenParameter.ExpiryMinutes = tenant.JwtAccessTokenExpiryMinutes;
            tokenParameter.Issuer = tenant.ApiUrl;
            tokenParameter.Audience = tenant.TenantDomainUrl;
            tokenParameter.TenantGuid = tenant.TenantGuid;
            tokenParameter.TenantClientId = tenant.TenantClientId;
            tokenParameter.UserName = user.LoginId;
            tokenParameter.UserSyncId = user.UserSyncId != null ? (Guid)user.UserSyncId : Guid.Empty;
            //tokenParameter.DateOfBirth = DateOnly.FromDateTime((DateTime)user.DOB);
            tokenParameter.DateOfBirth = user.DOB!=null ? ((DateTime)user.DOB).Date : null;
            //tokenParameter.Groups = groups;
            tokenParameter.Roles = roles;
            //tokenParameter.MetaRoles = metaRoles;
            tokenParameter.AbacRoles = abacRoles;
            tokenParameter.ClubsIn = clubsIn;
            tokenParameter.ClubsAdminOf = clubsAdminOf;
            //tokenParameter.Memberships = memberships;
            tokenParameter.FamilyMembers = familyMembers;

            // Generate Jwt token (Jwt)
            //return GenerateAccessToken(tokenParameter);
            // Generate encrypted token (Jwe)
            var token = _jweTokenService.GenerateEncryptedAccessToken(tokenParameter);

            const int maxCookieSize = 4096;

            // If token exceeds size limit, regenerate without club claims
            if (token.Length > maxCookieSize)
            {
                tokenParameter.ClubsIn = new List<string>();
                tokenParameter.ClubsAdminOf = new List<string>();
                token = _jweTokenService.GenerateEncryptedAccessToken(tokenParameter);
            }
            return token;
        }
        public string GenerateAccessToken(JwtTokenParameter tokenParameter)
        {
            var issuedAt = DateTime.UtcNow;
            var tokenExpiryTimeStamp = issuedAt.AddMinutes(tokenParameter.ExpiryMinutes);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenParameter.SecretKey));

            var claims = new List<Claim>
            {
                new Claim("UniqueGuid", Guid.NewGuid().ToString()),
                new Claim("UserSyncId", tokenParameter.UserSyncId.ToString()),
                new Claim("DateOfBirth", tokenParameter.DateOfBirth.ToString()),
                new Claim("TenantGuid", tokenParameter.TenantGuid.ToString()),
                new Claim("TenantClientId", tokenParameter.TenantClientId)//,
                //new Claim("memberships", JsonConvert.SerializeObject(tokenParameter.Memberships))
            };
            //foreach (var group in tokenParameter.Groups)
            //{
            //    claims.Add(new Claim("group", group));
            //}
            foreach (var role in tokenParameter.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            //foreach (var metaRole in tokenParameter.MetaRoles)
            //{
            //    claims.Add(new Claim("meta_role", metaRole));
            //}
            foreach (var abacRole in tokenParameter.AbacRoles)
            {
                claims.Add(new Claim("abac_role", abacRole));
            }
            foreach (var clubsIn in tokenParameter.ClubsIn)
            {
                claims.Add(new Claim("clubs_in", clubsIn));
            }
            foreach (var clubsAdminOf in tokenParameter.ClubsAdminOf)
            {
                claims.Add(new Claim("clubs_admin_of", clubsAdminOf));
            }
            //foreach (var clubsAdminOfWithChild in tokenParameter.ClubsAdminOfWithChild)
            //{
            //    claims.Add(new Claim("clubs_admin_of_with_child", clubsAdminOfWithChild));
            //}
            foreach (var familyMember in tokenParameter.FamilyMembers)
            {
                claims.Add(new Claim("family_members", familyMember));
            }
            var claimsIdentity = new ClaimsIdentity(claims);
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Issuer = tokenParameter.Issuer,
                Audience = tokenParameter.Audience,
                IssuedAt = issuedAt,
                Expires = tokenExpiryTimeStamp,
                SigningCredentials = signingCredentials
            };

            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);
            var token = jwtSecurityTokenHandler.WriteToken(securityToken);
            return token;
        }        
        public string GenerateRefreshToken(int size = 32)
        {
            var randomNumber = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
#if NET9_0_OR_GREATER
        public void AttachUserToContext(HttpContext context, string jwtToken, dynamic tenant)
        {
            jwtToken = _jweTokenService.CleanToken(jwtToken);
            if (_jweTokenService.IsJweToken(jwtToken)) // JWE token
            {
                var encryptedPrincipal = _jweTokenService.GetClaimsPrincipalFromEncryptedToken(jwtToken);
                if (encryptedPrincipal is not null)
                {
                    context.User = encryptedPrincipal;
                    return;
                }
            }
            else // JWT token
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = tenant.ApiUrl,
                    ValidateAudience = true,
                    ValidAudience = tenant.TenantDomainUrl,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tenant.JwtAccessTokenSecretKey))
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                context.User = principal;
            }
        }
#endif
        public ClaimsPrincipal? GetClaimsPrincipal(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken) || !jwtToken.StartsWith("Bearer "))
            {
                return null;
            }

            jwtToken = _jweTokenService.CleanToken(jwtToken);
            if (_jweTokenService.IsJweToken(jwtToken)) // JWE token
            {
                var encryptedPrincipal = _jweTokenService.GetClaimsPrincipalFromEncryptedToken(jwtToken);
                if (encryptedPrincipal is not null)
                {
                    return encryptedPrincipal;
                }
            }
            else // JWT token
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var tenantClientId = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken).Claims.First(c => c.Type == "TenantClientId").Value;
                var tenant = GetTenantByTenantClientId(tenantClientId);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = tenant?.ApiUrl,
                    ValidateAudience = true,
                    ValidAudience = tenant?.TenantDomainUrl,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tenant?.JwtAccessTokenSecretKey))
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                return principal;
            }
            return null;
        }
        public int SaveTokenIntoDB(int userId, Guid userSyncId, string accessToken, string refreshToken, int refreshTokenExpiryMinutes)
        {
#if NET9_0_OR_GREATER
            var (browser, version) = _utilityService.GetBrowserInfo(_httpContextAccessor);
            var connectionInfo = _utilityService.GetClientConnectionInfo(_httpContextAccessor);
#else
            var (browser, version) = _utilityService.GetBrowserInfo(HttpContext.Current.Request);
            var connectionInfo = _utilityService.GetClientConnectionInfo(HttpContext.Current.Request);
#endif
            string sql = @"[dbo].[SaveUserDeviceSessionInfo]";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Id", 0, dbType: DbType.Int64);
            queryParameters.Add("@UserId", userId, dbType: DbType.Int32);
            queryParameters.Add("@UserSyncId", userSyncId, dbType: DbType.Guid);
            queryParameters.Add("@UserSessionId", accessToken);
            queryParameters.Add("@UserDeviceModel", null);
            queryParameters.Add("@UserBrowserName", browser);
            queryParameters.Add("@UserBrowserVersion", version);
            queryParameters.Add("@UserDeviceIP", connectionInfo.IpAddress);
            queryParameters.Add("@UserDevicePort", connectionInfo.Port);
#if NET9_0_OR_GREATER
            queryParameters.Add("@UserDeviceName", _utilityService.GetDeviceType(_httpContextAccessor));
#else
            queryParameters.Add("@UserDeviceName", _utilityService.GetDeviceType(HttpContext.Current.Request));
#endif
            queryParameters.Add("@RefreshToken", _utilityService.EncryptData(refreshToken));
            queryParameters.Add("@RefreshTokenExpiryMinutes", refreshTokenExpiryMinutes, dbType: DbType.Int32);
            queryParameters.Add("@RefreshTokenExpiryDate", _utilityService.EncryptData(DateTime.UtcNow.AddMinutes(refreshTokenExpiryMinutes).ToString()));
            queryParameters.Add("@UserLocation", null);
            return _writeRepository.Value.Execute(sql, queryParameters);
        }
        public dynamic? GetTenantByTenantClientId(string tenantClientId)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT * FROM [dbo].[Tenants]
                               WHERE [TenantClientId]=@TenantClientId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantClientId", tenantClientId);
            return _readRepository.GetLazyRepository<dynamic>().Value.Get(sql, queryParameters, null, "text");
        }
        private UserClaimsData GetUserClaimsDataAsync(string loginId)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", loginId);

            using (var multipleResults = _readRepository.GetLazyRepository<dynamic>().Value.GetMultipleQuery(
                userClaimSql,
                queryParameters,
                null,
                "text"))
            {
                var result = new UserClaimsData();

                // Read User (Result Set 1)
                var users = multipleResults.Read<dynamic>();
                result.User = users.FirstOrDefault();

                // Read Roles (Result Set 2)
                var roles = multipleResults.Read<dynamic>();
                result.Roles = roles.Select(r => (string)r.Name).ToList();

                // Read ABAC Roles (Result Set 3)
                var abacRoles = multipleResults.Read<dynamic>();
                result.AbacRoles = abacRoles.Select(r => (string)r.Name).ToList();

                // Read Clubs In (Result Set 4)
                var clubsIn = multipleResults.Read<dynamic>();
                result.ClubsIn = clubsIn.Select(c => (string)c.SyncGuid).ToList();

                // Read Clubs Admin Of (Result Set 5)
                var clubsAdminOf = multipleResults.Read<dynamic>();
                result.ClubsAdminOf = clubsAdminOf.Select(c => (string)c.SyncGuid).ToList();

                // Read Family Members (Result Set 6)
                var familyMembers = multipleResults.Read<dynamic>();
                result.FamilyMembers = familyMembers.Select(f => ((Guid)f.UserSyncId).ToString()).ToList();

                return result;
            }
        }
        private dynamic? GetUserByLoginId(string loginId)
        {
            string sql = @"SELECT *
                          FROM [dbo].[User]
                          WHERE [LoginId]=@LoginId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", loginId);
            return _readRepository.GetLazyRepository<dynamic>().Value.Get(sql, queryParameters, null, "text");
        }
        private dynamic? GetUserByUserSyncId(string userSyncId)
        {
            string sql = @"SELECT *
                          FROM [dbo].[User]
                          WHERE [UserSyncId]=@UserSyncId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", new Guid(userSyncId), dbType: DbType.Guid);
            return _readRepository.GetLazyRepository<dynamic>().Value.Get(sql, queryParameters, null, "text");
        }
        
        private List<string> GetGroupsByUserId(int userId)
        {
            string sql = @"SELECT g.[Name]
                                FROM [dbo].[Group] g 
	                                INNER JOIN [dbo].[GroupMembers] gm
		                                ON g.GroupId=gm.GroupId
	                                INNER JOIN [dbo].[User] u
		                                ON u.Userid=gm.UserId
                                WHERE g.IsActive=1
	                                AND u.Userid=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => (string)x.Name).ToList();
        }
        private List<string> GetRolesByUser(string loginId)
        {
            string sql = @"SELECT DISTINCT r.[Name] FROM [dbo].[Role] r
	                                INNER JOIN [dbo].[RoleMembers] rm ON r.RoleId=rm.RoleId
	                                INNER JOIN [dbo].[User] u ON u.Userid=rm.UserId
                                WHERE u.LoginId=@LoginId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", loginId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => (string)x.Name).ToList();
        }
        private List<string> GetMetaRolesByUser(int userId)
        {
            string sql = @"SELECT DISTINCT RoleName AS [Name] FROM [dbo].[ClubMemberRoles]
                                WHERE UserId=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => (string)x.Name).ToList();
        }
        private List<string> GetAbacRolesByUser(int userId)
        {
            string sql = @"SELECT DISTINCT r.[Id],r.[Name],r.[Description],r.[Status]
                                FROM [dbo].[AbacRoles] r
	                                INNER JOIN [dbo].[AbacUserRoles] ur ON r.[Id]=ur.[RoleId]
                                WHERE ur.[UserId]=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => (string)x.Name).ToList();
        }
        private List<string> GetClubsByUserId(int userId)
        {
            string sql = @"SELECT d.SyncGuid FROM [dbo].[Document] d
	                            INNER JOIN [dbo].[Hierarchies] h ON h.EntityId=d.DocId
	                            INNER JOIN [dbo].[HierarchyLinks] hl ON hl.HierarchyId=h.Id
	                            INNER JOIN [User] u ON u.Userid = hl.UserId
                            WHERE d.RepositoryId=2
	                            AND u.Userid=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => (string)x.SyncGuid).ToList();
        }
        private List<string> GetClubsAdminByUserId(int userId)
        {
            string sql = @"SELECT d.SyncGuid FROM [dbo].[Document] d
	                            INNER JOIN [dbo].[ClubMemberRoles] cmr ON cmr.ClubDocId=d.DocId
	                            INNER JOIN [dbo].[Lookup_22] l22 ON l22.RowId=cmr.RoleId
	                            INNER JOIN [User] u ON u.Userid = cmr.UserId
                            WHERE d.RepositoryId=2
	                            AND l22.field_101 = 'Yes'
	                            AND u.Userid=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => (string)x.SyncGuid).ToList();
        }
        private List<Membership> GetMembershipByUserId(int userId)
        {
            string sql = @"SELECT 
	                        d.SyncGuid,
                            JSON_VALUE(membership.value, '$.name') AS [Name],
                            JSON_VALUE(membership.value, '$.category') AS Category
                        FROM [dbo].[MembershipSummary] m
                            CROSS APPLY OPENJSON(m.[Entity1Memberships]) AS JsonData
                            CROSS APPLY OPENJSON(JsonData.value, '$.memberships') AS membership
                            INNER JOIN [dbo].[Document] d ON JSON_VALUE(membership.value, '$.membershipDocId')=d.DocId
                            INNER JOIN [User] u ON u.MemberDocId = m.[EntityId]
                        WHERE u.Userid = @UserId
                            AND JSON_VALUE(membership.value, '$.status') = '1'
                            AND d.RepositoryId = 21";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => new Membership
            {
                SyncGuid = (string)x.SyncGuid,
                Name = (string)x.Name,
                Category = (string)x.Category
            }).ToList();
        }
        private List<string> GetFamilyMembersByUserId(int userId)
        {
            string sql = @"SELECT u.UserSyncId FROM [User] u
	                                INNER JOIN Family_Links fl ON u.MemberDocId=fl.Entityid
	                                INNER JOIN Family_Default fd ON fl.DocId=fd.DocId
                                WHERE fl.DocId IN (SELECT DocId FROM Family_Links flm INNER JOIN [User] us 
                                ON flm.Entityid=us.MemberDocId WHERE us.Userid=@UserId)";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var result = _readRepository.GetLazyRepository<dynamic>().Value.GetList(sql, queryParameters, null, "text").ToList();
            return result.Select(x => ((Guid)x.UserSyncId).ToString()).ToList();
        }
       



    }
}
