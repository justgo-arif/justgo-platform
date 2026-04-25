using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using Konscious.Security.Cryptography;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#else
using System.Web;
using System.IO;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace JustGo.Authentication.Infrastructure.Utilities
{
    public class UtilityService : IUtilityService
    {        
        // For hashing passwords using Argon2id
        private const int ARGON2_SALT_SIZE = 16;        // 128 bits
        private const int ARGON2_HASH_SIZE = 32;        // 256 bits
        private const int ARGON2_ITERATIONS = 4;        // OWASP recommended
        private const int ARGON2_MEMORY_SIZE = 65536;   // 64 MB (OWASP recommended)
        private const int ARGON2_PARALLELISM = 8;       // 8 threads
        /////////////////////////////////
        ///For AES-256-GCM encryption algorithm
        private const string SEncryptionKey = "!#$a45?7";
        private const int AES_GCM_NONCE_SIZE = 12; // 96 bits (recommended)
        private const int AES_GCM_TAG_SIZE = 16;   // 128 bits
        private const int AES_GCM_KEY_SIZE = 32;   // 256 bits
        //////////////////////////////////////
        private readonly IJweTokenService _jweTokenService;
#if NET9_0_OR_GREATER
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UtilityService(IHttpContextAccessor httpContextAccessor, IReadRepositoryFactory readRepository, IJweTokenService jweTokenService)
        {
            _httpContextAccessor = httpContextAccessor;
            _readRepository = readRepository;
            _jweTokenService = jweTokenService;
        }
#else
        public UtilityService(IJweTokenService jweTokenService)
        {
            _jweTokenService = jweTokenService;
        }
#endif
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            try
            {
                // Generate cryptographically secure random salt
#if NET9_0_OR_GREATER
                // Use modern .NET 9 API
                byte[] salt = RandomNumberGenerator.GetBytes(ARGON2_SALT_SIZE);
#else
                // Use .NET Framework 4.8.1 API
                byte[] salt = new byte[ARGON2_SALT_SIZE];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
#endif              
               
                // Hash password with Argon2id
                byte[] hash = HashPasswordWithArgon2id(password, salt);

                // Combine salt + hash for storage (salt:16 bytes, hash:32 bytes = 48 bytes total)
                byte[] hashWithSalt = new byte[ARGON2_SALT_SIZE + ARGON2_HASH_SIZE];
                Buffer.BlockCopy(salt, 0, hashWithSalt, 0, ARGON2_SALT_SIZE);
                Buffer.BlockCopy(hash, 0, hashWithSalt, ARGON2_SALT_SIZE, ARGON2_HASH_SIZE);

                return Convert.ToBase64String(hashWithSalt);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during password hashing", ex);
            }
        }
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                // Decode stored hash
                byte[] hashWithSalt = Convert.FromBase64String(hashedPassword);

                // Validate length
                if (hashWithSalt.Length != ARGON2_SALT_SIZE + ARGON2_HASH_SIZE)
                    return false;

                // Extract salt and hash
                byte[] salt = new byte[ARGON2_SALT_SIZE];
                byte[] storedHash = new byte[ARGON2_HASH_SIZE];
                Buffer.BlockCopy(hashWithSalt, 0, salt, 0, ARGON2_SALT_SIZE);
                Buffer.BlockCopy(hashWithSalt, ARGON2_SALT_SIZE, storedHash, 0, ARGON2_HASH_SIZE);

                // Hash the input password with the same salt
                byte[] newHash = HashPasswordWithArgon2id(password, salt);

                // Constant-time comparison (prevents timing attacks)
#if NET9_0_OR_GREATER
                return CryptographicOperations.FixedTimeEquals(newHash, storedHash);
#else
                return ConstantTimeEquals(newHash, storedHash);
#endif
            }
            catch
            {
                return false;
            }
        }
        private byte[] HashPasswordWithArgon2id(string password, byte[] salt)
        {
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = ARGON2_PARALLELISM;  // 8 threads
                argon2.MemorySize = ARGON2_MEMORY_SIZE;           // 64 MB
                argon2.Iterations = ARGON2_ITERATIONS;             // 4 iterations

                return argon2.GetBytes(ARGON2_HASH_SIZE);
            }
        }
        private bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return false;

            // Length check is safe to short-circuit
            if (a.Length != b.Length)
                return false;

            // XOR all bytes and accumulate differences
            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                // Use bitwise OR to accumulate any differences
                // This ensures all bytes are always compared
                result |= a[i] ^ b[i];
            }

            // If result is 0, all bytes were identical
            return result == 0;
        }
        public string EncryptData2(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

            try
            {
                // Generate random salt for key derivation
#if NET9_0_OR_GREATER
                // Use modern .NET 9 API
                byte[] salt = RandomNumberGenerator.GetBytes(16);
#else
                // Use .NET Framework 4.8.1 API
                byte[] salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
#endif 

                // Derive a secure 256-bit key using PBKDF2
                byte[] key = DeriveKeyFromPassword(SEncryptionKey, salt);

                // Generate random nonce (96 bits recommended for GCM)                
#if NET9_0_OR_GREATER
                // Use modern .NET 9 API
                byte[] nonce = RandomNumberGenerator.GetBytes(AES_GCM_NONCE_SIZE);
#else
                // Use .NET Framework 4.8.1 API
                byte[] nonce = new byte[AES_GCM_NONCE_SIZE];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }
#endif 
                // Convert plaintext to bytes
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

#if NET9_0_OR_GREATER
                // ==================== .NET 9: Use native AesGcm ====================
                byte[] cipherBytes = new byte[plainBytes.Length];
                // Allocate space for the authentication tag
                byte[] tag = new byte[AES_GCM_TAG_SIZE];
                // Encrypt using AES-GCM
                using (var aesGcm = new AesGcm(key, AES_GCM_TAG_SIZE))
                {
                    aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
                }
#else
// ==================== .NET Framework 4.8.1: Use BouncyCastle ====================
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(
                    new KeyParameter(key),
                    AES_GCM_TAG_SIZE * 8, // Tag size in bits (128 bits)
                    nonce,
                    null); // No additional authenticated data

                cipher.Init(true, parameters); // true = encrypt

                // Allocate space for ciphertext + tag
                byte[] output = new byte[cipher.GetOutputSize(plainBytes.Length)];
                int len = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
                cipher.DoFinal(output, len);

                // Extract cipherBytes and tag from output
                byte[] cipherBytes = new byte[plainBytes.Length];
                byte[] tag = new byte[AES_GCM_TAG_SIZE];
                Buffer.BlockCopy(output, 0, cipherBytes, 0, cipherBytes.Length);
                Buffer.BlockCopy(output, cipherBytes.Length, tag, 0, AES_GCM_TAG_SIZE);
#endif 
                // Combine: salt (16) + nonce (12) + tag (16) + ciphertext (variable)
                byte[] result = new byte[salt.Length + nonce.Length + tag.Length + cipherBytes.Length];
                Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                Buffer.BlockCopy(nonce, 0, result, salt.Length, nonce.Length);
                Buffer.BlockCopy(tag, 0, result, salt.Length + nonce.Length, tag.Length);
                Buffer.BlockCopy(cipherBytes, 0, result, salt.Length + nonce.Length + tag.Length, cipherBytes.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during AES-GCM encryption", ex);
            }
        }
        public string DecryptData2(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

            try
            {
                byte[] fullData = Convert.FromBase64String(encryptedData);

                // Validate minimum size: salt(16) + nonce(12) + tag(16) = 44 bytes minimum
                if (fullData.Length < 44)
                    throw new CryptographicException("Invalid encrypted data format");

                // Extract components
                byte[] salt = new byte[16];
                byte[] nonce = new byte[AES_GCM_NONCE_SIZE];
                byte[] tag = new byte[AES_GCM_TAG_SIZE];
                byte[] cipherBytes = new byte[fullData.Length - 44];

                Buffer.BlockCopy(fullData, 0, salt, 0, 16);
                Buffer.BlockCopy(fullData, 16, nonce, 0, AES_GCM_NONCE_SIZE);
                Buffer.BlockCopy(fullData, 28, tag, 0, AES_GCM_TAG_SIZE);
                Buffer.BlockCopy(fullData, 44, cipherBytes, 0, cipherBytes.Length);

                // Derive the same key
                byte[] key = DeriveKeyFromPassword(SEncryptionKey, salt);

#if NET9_0_OR_GREATER
                // ==================== .NET 9: Use native AesGcm ====================
                // Decrypt and verify authentication tag
                byte[] plainBytes = new byte[cipherBytes.Length];
                using (var aesGcm = new AesGcm(key, AES_GCM_TAG_SIZE))
                {
                    aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
                }
#else
                // ==================== .NET Framework 4.8.1: Use BouncyCastle ====================
                 var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(
                    new KeyParameter(key),
                    AES_GCM_TAG_SIZE * 8,
                    nonce,
                    null);

                cipher.Init(false, parameters); // false = decrypt

                // Combine ciphertext + tag for BouncyCastle
                byte[] input = new byte[cipherBytes.Length + tag.Length];
                Buffer.BlockCopy(cipherBytes, 0, input, 0, cipherBytes.Length);
                Buffer.BlockCopy(tag, 0, input, cipherBytes.Length, tag.Length);

                // Decrypt
                byte[] output = new byte[cipher.GetOutputSize(input.Length)];
                int len = cipher.ProcessBytes(input, 0, input.Length, output, 0);
                cipher.DoFinal(output, len);

                // Return only the plaintext portion
                byte[] plainBytes = new byte[len];
                Buffer.BlockCopy(output, 0, plainBytes, 0, len);
#endif
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException ex)
            {
                // Authentication tag verification failed - data was tampered with
                throw new CryptographicException("Decryption failed. Data may be corrupted or tampered.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during AES-GCM decryption", ex);
            }
        }
        private byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                100000, // 100k iterations (OWASP recommendation)
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(AES_GCM_KEY_SIZE);
            }
        }
        public string Encrypt(string stringToEncrypt)
        {
            if (string.IsNullOrEmpty(stringToEncrypt))
            {
                return null;
            }
            byte[] key = { };
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };
            byte[] inputByteArray; //Convert.ToByte(stringToEncrypt.Length)

            try
            {
                key = Encoding.UTF8.GetBytes(SEncryptionKey.Substring(0, 8));
                var des = new DESCryptoServiceProvider();
                inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
                var ms = new MemoryStream();
                var cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();



                if (Convert.ToBase64String(ms.ToArray()).LastIndexOf('=') > 0)
                    return Convert.ToBase64String(ms.ToArray()).Remove(Convert.ToBase64String(ms.ToArray()).LastIndexOf('=')) + "z";
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
        // Advance Encryption

        public string EncryptData(string strData)
        {
            // Define key and IV
            byte[] key = Encoding.UTF8.GetBytes(SEncryptionKey);
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };

            try
            {
                // Create DES provider and streams
                using (var des = new DESCryptoServiceProvider())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, des.CreateEncryptor(key, IV), CryptoStreamMode.Write))
                {
                    byte[] inputByteArray = Encoding.UTF8.GetBytes(strData);
                    cryptoStream.Write(inputByteArray, 0, inputByteArray.Length);
                    cryptoStream.FlushFinalBlock();

                    // Return the encrypted string
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                // Log exception or throw a more specific exception as needed
                throw new Exception("Error during encryption", ex);
            }
        }

        // Advance Decryption

        public string DecryptData(string strData)
        {
            // Define key and IV
            byte[] key = Encoding.UTF8.GetBytes(SEncryptionKey);
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };

            try
            {
                byte[] inputByteArray = Convert.FromBase64String(strData);

                // Create DES provider and streams
                using (var des = new DESCryptoServiceProvider())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, des.CreateDecryptor(key, IV), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(inputByteArray, 0, inputByteArray.Length);
                    cryptoStream.FlushFinalBlock();

                    // Return the decrypted string
                    return Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                // Log exception or throw a more specific exception as needed
                throw new Exception("Error during decryption", ex);
            }
        }
#if NET9_0_OR_GREATER
        public async Task<int> GetCurrentUserId(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var claims = context?.User.Claims.ToList();
            var userSyncId = claims?.FirstOrDefault(c => c.Type == "UserSyncId")?.Value ?? throw new InvalidOperationException("UserSyncId claim not found");
            string sql = "SELECT TOP 1 [Userid] FROM [dbo].[User] WHERE [UserSyncId]=@UserSyncId";
            var result = await _readRepository.GetLazyRepository<object>().Value.GetSingleAsync<int>(sql, new { UserSyncId = userSyncId }, null, cancellationToken, "text");
            if(result == 0) throw new InvalidOperationException($"User not found for UserSyncId: {userSyncId}");
            return result;
        }

        public async Task<CurrentUser> GetCurrentUser(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var claims = context?.User.Claims.ToList();
            var userSyncId = claims?.FirstOrDefault(c => c.Type == "UserSyncId")?.Value ?? throw new InvalidOperationException("UserSyncId claim not found");
            string sql = """
                Select TOP 1 UserId, LoginId, FirstName, LastName, IsActive, IsLocked, MemberDocId, MemberId
                from [User] WHERE UserSyncId = @UserSyncId
                """;
            var result = await _readRepository.GetLazyRepository<CurrentUser>().Value.GetAsync(sql, cancellationToken, new { UserSyncId = userSyncId }, null, "text");
            if (result is null) throw new InvalidOperationException($"User not found for UserSyncId: {userSyncId}");
            return result;
        }

        public async Task<CurrentUser?> GetCurrentUserPublic(CancellationToken cancellationToken)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                var authHeader = context?.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return null; // No token provided
                }

                var token = _jweTokenService.CleanToken(authHeader);

                var userSyncId = ExtractUserSyncIdFromToken(token);

                if (string.IsNullOrEmpty(userSyncId))
                {
                    return null;
                }

                string sql = """
                Select TOP 1 UserId, LoginId, FirstName, LastName, IsActive, IsLocked, MemberDocId, MemberId
                from [User] WHERE UserSyncId = @UserSyncId
                """;

                var result = await _readRepository.GetLazyRepository<CurrentUser>().Value.GetAsync(sql, cancellationToken, new { UserSyncId = userSyncId }, null, "text");
                return result;
            }
            catch
            {
                return null;
            }
        }

        private string? ExtractUserSyncIdFromToken(string token)
        {
            try
            {
                return _jweTokenService.GetClaimFromTokenByType(token, "UserSyncId");
            }
            catch
            {
                return null;
            }
        }

        public Guid GetCurrentUserGuid()
        {
            var context = _httpContextAccessor.HttpContext;
            var userSyncIdValue = context?.User?.Claims?.FirstOrDefault(c => c.Type == "UserSyncId")?.Value
                ?? throw new InvalidOperationException("UserSyncId claim not found");

            if (!Guid.TryParse(userSyncIdValue, out var userSyncId))
                throw new InvalidOperationException("Invalid UserSyncId format");

            return userSyncId;
        }

        public async Task<string?> GetCurrentTenantGuid(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var tenantId = context?.User?.Claims?.FirstOrDefault(c => c.Type == "TenantGuid")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }
            var tenantClientId = context?.Items["tenantClientId"];
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT [TenantGuid] FROM [dbo].[Tenants]
                               WHERE [TenantClientId]=@TenantClientId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantClientId", tenantClientId);
            var result = await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text");
            tenantId = result?.ToString();
            return tenantId;
        }        
        public async Task<string?> GetTenantClientIdByDomain(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var tenantClientId = context?.User?.Claims?.FirstOrDefault(c => c.Type == "TenantClientId")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantClientId))
            {
                return tenantClientId;
            }
            tenantClientId = context?.Request.Headers["X-Tenant-Client-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantClientId))
            {
                return tenantClientId;
            }
            string? domain = context?.Request.Headers["Origin"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(domain))
            {
                var referer = context?.Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                {
                    domain = $"{uri.Scheme}://{uri.Host}";
                }
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                tenantClientId = context?.Items["tenantClientId"]?.ToString();
                return tenantClientId;
            }

            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT TOP 1 [TenantClientId] FROM [dbo].[Tenants]
                  WHERE [TenantDomainUrl] LIKE '%' + @TenantDomainUrl + '%'";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantDomainUrl", domain);
            var result = await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text");
            return result as string;
        }
        public async Task<int?> GetUserIdByMemberIdAsync(string memberId, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT TOP 1 Userid
                               FROM [dbo].[User]
                               WHERE MemberId = @memberId
                               """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("memberId", memberId);
            var repo = _readRepository.GetRepository<object>();
            var result = await repo.GetSingleAsync<int?>(sql, queryParameters, null, cancellationToken, QueryType.Text);
            return result;
        }
        public async Task<int?> GetUserIdByMemberDocIdAsync(string memberDocId, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT TOP 1 Userid
                               FROM [dbo].[User]
                               WHERE MemberDocId = @memberDocId
                               """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("memberDocId", memberDocId);
            var repo = _readRepository.GetRepository<object>();
            var result = await repo.GetSingleAsync<int?>(sql, queryParameters, null, cancellationToken, QueryType.Text);
            return result;
        }
        public string GetDeviceType(IHttpContextAccessor httpContextAccessor)
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            if (string.IsNullOrWhiteSpace(userAgent))
                return "Unknown";

            var lowerUserAgent = userAgent.ToLowerInvariant();

            // Mobile devices (most specific first)
            if (lowerUserAgent.Contains("iphone"))
                return "Mobile (iPhone, iOS)";

            if (lowerUserAgent.Contains("ipod"))
                return "Mobile (iPod Touch, iOS)";

            // Android mobile detection - check for "mobile" keyword
            if (lowerUserAgent.Contains("android") && lowerUserAgent.Contains("mobile"))
                return "Mobile (Android)";

            // Windows Phone
            if (lowerUserAgent.Contains("windows phone") || lowerUserAgent.Contains("iemobile"))
                return "Mobile (Windows Phone)";

            // BlackBerry
            if (lowerUserAgent.Contains("blackberry") || lowerUserAgent.Contains("bb10"))
                return "Mobile (BlackBerry)";

            // Generic mobile indicators
            if (lowerUserAgent.Contains("mobile") &&
                (lowerUserAgent.Contains("safari") || lowerUserAgent.Contains("chrome")))
                return "Mobile (Generic)";

            // Tablets (after mobile detection)
            if (lowerUserAgent.Contains("ipad"))
                return "Tablet (iPad, iOS)";

            // Android tablets (Android without "mobile")
            if (lowerUserAgent.Contains("android"))
                return "Tablet (Android)";

            // Surface tablets
            if (lowerUserAgent.Contains("windows") && lowerUserAgent.Contains("touch"))
                return "Tablet (Windows)";

            // Kindle
            if (lowerUserAgent.Contains("kindle") || lowerUserAgent.Contains("silk"))
                return "Tablet (Kindle)";

            // Desktop operating systems
            if (lowerUserAgent.Contains("windows nt"))
                return "Desktop (Windows)";

            if (lowerUserAgent.Contains("mac os x") && !lowerUserAgent.Contains("mobile"))
                return "Desktop (macOS)";

            if (lowerUserAgent.Contains("linux") && !lowerUserAgent.Contains("android"))
                return "Desktop (Linux)";

            if (lowerUserAgent.Contains("chromeos"))
                return "Desktop (Chrome OS)";

            // Smart TV and gaming consoles
            if (lowerUserAgent.Contains("smart-tv") || lowerUserAgent.Contains("smarttv") ||
                lowerUserAgent.Contains("googletv") || lowerUserAgent.Contains("appletv"))
                return "Smart TV";

            if (lowerUserAgent.Contains("playstation") || lowerUserAgent.Contains("xbox") ||
                lowerUserAgent.Contains("nintendo"))
                return "Gaming Console";

            // Bots and crawlers
            if (lowerUserAgent.Contains("bot") || lowerUserAgent.Contains("crawler") ||
                lowerUserAgent.Contains("spider") || lowerUserAgent.Contains("crawl"))
                return "Bot/Crawler";

            return "Other";
        }

        public (string Browser, string Version) GetBrowserInfo(IHttpContextAccessor httpContextAccessor)
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            if (!string.IsNullOrEmpty(userAgent))
            {
                var browserInfo = ParseUserAgent(userAgent);
                return browserInfo;
            }

            return ("Unknown", "Unknown");
        }
        public ClientConnectionInfo GetClientConnectionInfo(IHttpContextAccessor httpContextAccessor)
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null)
                return new ClientConnectionInfo();

            var connectionInfo = new ClientConnectionInfo();

            // Try to get real client IP through headers first (most accurate for proxied environments)
            var realIp = GetRealClientIp(context);
            if (!string.IsNullOrEmpty(realIp))
            {
                connectionInfo.IpAddress = realIp;
                connectionInfo.IsProxied = true;
                connectionInfo.Source = "Proxy Headers";

                // Get proxy chain for debugging
                connectionInfo.ProxyChain = GetProxyChain(context);
            }
            else
            {
                // Fallback to direct connection
                connectionInfo.IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                connectionInfo.Port = context.Connection.RemotePort.ToString();
                connectionInfo.Source = "Direct Connection";
                connectionInfo.IsProxied = false;
            }

            // Clean and validate IP
            connectionInfo.IpAddress = CleanIpAddress(connectionInfo.IpAddress);

            return connectionInfo;
        }

        private string GetRealClientIp(HttpContext context)
        {
            // Order of preference for getting real client IP
            var headerNames = new[]
            {
                "CF-Connecting-IP",    // Cloudflare
                "X-Forwarded-For",     // Standard proxy header
                "X-Real-IP",           // Nginx
                "X-Client-IP",         // Apache
                "X-Cluster-Client-IP", // Cluster environments
                "Forwarded-For",       // Alternative
                "Forwarded"            // RFC 7239
            };

            foreach (var headerName in headerNames)
            {
                if (context.Request.Headers.TryGetValue(headerName, out var headerValue))
                {
                    var ip = ExtractFirstValidIp(headerValue.ToString());
                    if (!string.IsNullOrEmpty(ip))
                        return ip;
                }
            }

            return null;
        }

        private List<string> GetProxyChain(HttpContext context)
        {
            var chain = new List<string>();

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                chain.AddRange(ips.Select(ip => ip.Trim()));
            }

            return chain;
        }
        public string GetClientIpAddress(IHttpContextAccessor httpContextAccessor)
        {
            var connectionInfo = GetClientConnectionInfo(httpContextAccessor);
            return connectionInfo.IpAddress;
        }

        public string GetClientPort(IHttpContextAccessor httpContextAccessor)
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Connection?.RemotePort != null)
            {
                return context.Connection.RemotePort.ToString();
            }
            return "Unknown";
        }
        public async Task<int> GetOwnerIdAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                               DECLARE @regionValue NVARCHAR(50) = 'Region';
                               SELECT @regionValue = Value 
                               FROM SystemSettings 
                               WHERE ItemKey = 'ORGANISATION.REGIONAL_ENTITY_IDENTITY';
                               
                               WITH RegionalOwner AS (
                                   SELECT rc.DocId AS OwnerId
                                   FROM Clubs_Default rc
                                   INNER JOIN ClubMemberRoles cmr ON cmr.ClubDocId = rc.DocId
                                   INNER JOIN [User] u ON u.MemberDocId = cmr.MemberDocId
                                   INNER JOIN lookup_22 l22 ON cmr.rolename = l22.field_100 AND l22.field_101 = 'yes'
                                   WHERE u.userid = @UserId AND rc.clubtype = @regionValue
                               )
                               SELECT top 1
                                   CASE 
                                       -- Check if user exists in groups 1 or 25
                                       WHEN EXISTS (
                                           SELECT 1 
                                           FROM [User] u 
                                           INNER JOIN groupmembers gm ON u.userid = gm.userid 
                                           WHERE u.userid = @UserId AND gm.GroupId IN (1, 25)
                                       ) THEN 0
                                       
                                       -- Check if user exists in groups 26 or 27 AND has regional role
                                       WHEN EXISTS (
                                           SELECT 1 
                                           FROM [User] u 
                                           INNER JOIN groupmembers gm ON u.userid = gm.userid 
                                           WHERE u.userid = @UserId AND gm.GroupId IN (26, 27)
                                       ) AND EXISTS (SELECT 1 FROM RegionalOwner) 
                                       THEN (SELECT TOP 1 OwnerId FROM RegionalOwner)
                                       
                                       -- Default case
                                       ELSE -1
                                   END AS OwnerId;
                               """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", await GetCurrentUserId(cancellationToken));
            var ownerId = await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync<int>(sql, queryParameters, null, cancellationToken, QueryType.Text);
            return ownerId;

        }

        public async Task<int> GetOwnerIdByGuid(string ownerGuid, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT CASE 
                                   WHEN EXISTS (
                                       SELECT 1 
                                       FROM merchantprofile_default mpd 
                                       INNER JOIN Document d ON d.docid = mpd.docid
                                       WHERE d.syncguid = @ownerGuid and mpd.Merchanttype = 'NGB'
                                   ) THEN 0
                                   WHEN EXISTS (
                                       SELECT 1 
                                       FROM SystemSettings 
                                       WHERE itemkey = 'ORGANISATION.ENABLE_MEMBER_GRID_MODULE' 
                                       AND Value = 'true'
                                   ) THEN ISNULL((
                                       SELECT TOP 1 H.EntityId
                                       FROM Hierarchies H 
                                       INNER JOIN Document D ON D.DocId = H.EntityId
                                       WHERE D.SyncGuid = @ownerGuid
                                   ), -1)
                                   ELSE ISNULL((
                                       SELECT TOP 1 D.DocId 
                                       FROM Clubs_Default C 
                                       INNER JOIN Document D ON D.DocId = C.DocId
                                       WHERE D.SyncGuid = @ownerGuid
                                   ), -1)
                               END AS OwnerId;
                               """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ownerGuid", ownerGuid);

            var ownerId = await _readRepository.GetLazyRepository<object>().Value
                .GetSingleAsync<int>(sql, queryParameters, null, cancellationToken, QueryType.Text);

            return ownerId;
        }
        public async Task<int> GetUserIdByUserSyncGuidAsync(string userSyncGuid, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT TOP 1 Userid
                               FROM [dbo].[User]
                               WHERE UserSyncId = @UserSyncId
                               """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("UserSyncId", userSyncGuid);
            var repo = _readRepository.GetRepository<object>();
            var result = await repo.GetSingleAsync<int>(sql, queryParameters, null, cancellationToken, QueryType.Text);
            return result;
        }
        public async Task<bool> VerifyOwnerById(int ownerId, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT CASE 
                                   WHEN EXISTS (
                                       SELECT 1 
                                       FROM [User] u 
                                       INNER JOIN groupmembers gm ON u.userid = gm.userid 
                                       WHERE u.userid = @userId AND gm.GroupId IN (1, 25)
                                   ) THEN 1
                                   WHEN EXISTS (
                                       SELECT 1 
                                       FROM [User] u 
                                       INNER JOIN groupmembers gm ON u.userid = gm.userid 
                                       WHERE u.userid = @userId AND gm.GroupId IN (26, 27)
                                   ) 
                                   THEN 1
                                   ELSE 0
                               END AS flag;
                               """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ownerId", ownerId);
            queryParameters.Add("@userId", await GetCurrentUserId(cancellationToken));

            var flag = await _readRepository.GetLazyRepository<object>().Value
                .GetSingleAsync<bool>(sql, queryParameters, null, cancellationToken, QueryType.Text);
            return flag;
        }
        public async Task<List<Group>> SelectGroupByUserAsync(int userID, CancellationToken cancellationToken)
        {
            const string sql = @"
            SELECT g.GroupId, g.Name, g.Description, g.IsActive, g.Tag, gm.UserId
            FROM dbo.[Group] g
            INNER JOIN dbo.GroupMembers gm ON g.GroupId = gm.GroupId
            WHERE gm.UserId = @UserID";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserID", userID);
            var result = await _readRepository.GetLazyRepository<Group>().Value.GetListAsync(sql, cancellationToken, queryParameters, commandType: "text");
            return result.ToList();
        }
        public void SetCookie(string key, string value, double ExpiryMinutes)
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            if (response is not null)
            {
                //response.Headers.Append(key, value);

                response.Cookies.Append(key, value, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(ExpiryMinutes)
                });
            }
        }

        public async Task<int> GetTenantSportTypeAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                               select JSON_VALUE([Value], '$."SportTypeId"') as SportTypeId 
                               from SystemSettings Where ItemKey = 'Result.SportType'
                               """;
            var sportTypeId = await _readRepository.GetRepository<object>().GetSingleAsync<int?>(sql, null, 
                null, cancellationToken, QueryType.Text);

            if (sportTypeId is > 0)
            {
                return sportTypeId.Value;
            }

            throw new InvalidOperationException($"SportTypeId is not configured.");
        }


#endif
        public string? GetCurrentTenantClientId()
        {
            var tenantId = TenantContextManager.GetTenantClientId();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }
#if NET9_0_OR_GREATER
            var context = _httpContextAccessor.HttpContext;
            var tenantClientId = context?.User?.Claims?.FirstOrDefault(c => c.Type == "TenantClientId")?.Value;
#else
            var context = HttpContext.Current;
            HttpCookie httpCookie = context.Request.Cookies["Authorization"];
            var token = httpCookie?.Value;
            //var tenantClientId = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "TenantClientId").Value;
            var tenantClientId = _jweTokenService.GetClaimFromTokenByType(token ?? string.Empty, "TenantClientId");
#endif

            if (!string.IsNullOrWhiteSpace(tenantClientId))
            {
                return tenantClientId;
            }
            tenantClientId = context?.Items["tenantClientId"]?.ToString();
            return tenantClientId;
        }
              
        private (string Browser, string Version) ParseUserAgent(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return ("Unknown", "Unknown");

            var lowerUserAgent = userAgent.ToLowerInvariant();

            // Order is crucial - most specific browsers first

            // Edge (must come before Chrome as it contains "Chrome")
            if (lowerUserAgent.Contains("edg/") || lowerUserAgent.Contains("edge/"))
            {
                var match = Regex.Match(userAgent, @"(?:Edg|Edge)/(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return ("Edge", match.Groups[1].Value);
            }

            // Opera (must come before Chrome as it contains "Chrome")
            if (lowerUserAgent.Contains("opr/") || lowerUserAgent.Contains("opera/"))
            {
                var match = Regex.Match(userAgent, @"(?:OPR|Opera)/(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return ("Opera", match.Groups[1].Value);
            }

            // Firefox
            if (lowerUserAgent.Contains("firefox/"))
            {
                var match = Regex.Match(userAgent, @"Firefox/(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return ("Firefox", match.Groups[1].Value);
            }

            // Safari (must come before Chrome as Safari on macOS contains "Chrome")
            if (lowerUserAgent.Contains("safari/") && lowerUserAgent.Contains("version/") && !lowerUserAgent.Contains("chrome/"))
            {
                var match = Regex.Match(userAgent, @"Version/(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return ("Safari", match.Groups[1].Value);
            }

            // Chrome and Chrome-based browsers (must come last)
            if (lowerUserAgent.Contains("chrome/"))
            {
                var match = Regex.Match(userAgent, @"Chrome/(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return ("Chrome", match.Groups[1].Value);
            }

            // Internet Explorer
            if (lowerUserAgent.Contains("trident/") || lowerUserAgent.Contains("msie"))
            {
                var ieMatch = Regex.Match(userAgent, @"(?:MSIE\s|rv:)(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (ieMatch.Success)
                    return ("Internet Explorer", ieMatch.Groups[1].Value);
            }

            return ("Unknown", "Unknown");
        }
        private string ExtractFirstValidIp(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                return null;

            // Handle comma-separated IPs (X-Forwarded-For format)
            var ips = headerValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var ip in ips)
            {
                var cleanIp = ip.Trim();
                if (IsValidIpAddress(cleanIp) && !IsPrivateOrLocalIp(cleanIp))
                {
                    return cleanIp;
                }
            }

            // If no public IP found, return the first valid IP (might be private)
            foreach (var ip in ips)
            {
                var cleanIp = ip.Trim();
                if (IsValidIpAddress(cleanIp))
                {
                    return cleanIp;
                }
            }

            return null;
        }

        private string CleanIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return "Unknown";

            // Remove port if present (e.g., "192.168.1.1:8080" -> "192.168.1.1")
            if (ipAddress.Contains(':') && !ipAddress.Contains("::")) // Avoid IPv6
            {
                var parts = ipAddress.Split(':');
                if (parts.Length == 2 && IsValidIpAddress(parts[0]))
                {
                    return parts[0];
                }
            }

            return ipAddress;
        }

        private bool IsValidIpAddress(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }

        private bool IsPrivateOrLocalIp(string ip)
        {
            if (!System.Net.IPAddress.TryParse(ip, out var address))
                return false;

            var bytes = address.GetAddressBytes();

            // Check for private IP ranges
            return address.IsIPv6LinkLocal ||
                   (bytes.Length >= 4 && (
                       (bytes[0] == 10) ||
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168) ||
                       (bytes[0] == 127) || // Loopback
                       (bytes[0] == 169 && bytes[1] == 254) // Link-local
                   ));
        }
#if NET481
        public (string Browser, string Version) GetBrowserInfo(HttpRequest request)
        {
            var userAgent = request.UserAgent;

            if (!string.IsNullOrEmpty(userAgent))
            {
                var browserInfo = ParseUserAgent(userAgent);
                return browserInfo;
            }

            return ("Unknown", "Unknown");
        }
        public ClientConnectionInfo GetClientConnectionInfo(HttpRequest request)
        {
            var connectionInfo = new ClientConnectionInfo();

            if (System.Web.HttpContext.Current != null)
            {
                var Request = System.Web.HttpContext.Current.Request;

                // Try to get real client IP through headers first
                var realIp = GetRealClientIpFramework(Request);
                if (!string.IsNullOrEmpty(realIp))
                {
                    connectionInfo.IpAddress = realIp;
                    connectionInfo.IsProxied = true;
                    connectionInfo.Source = "Proxy Headers";
                    connectionInfo.ProxyChain = GetProxyChainFramework(Request);
                }
                else
                {
                    // Fallback to direct connection
                    connectionInfo.IpAddress = Request.UserHostAddress ?? "Unknown";
                    connectionInfo.Port = Request.ServerVariables["REMOTE_PORT"] ?? "Unknown";
                    connectionInfo.Source = "Direct Connection";
                    connectionInfo.IsProxied = false;
                }

                // Clean and validate IP
                connectionInfo.IpAddress = CleanIpAddress(connectionInfo.IpAddress);
            }

            return connectionInfo;
        }

        private string GetRealClientIpFramework(HttpRequest request)
        {
            // Order of preference for getting real client IP
            var headerNames = new[]
            {
                "HTTP_CF_CONNECTING_IP",    // Cloudflare
                "HTTP_X_FORWARDED_FOR",     // Standard proxy header
                "HTTP_X_REAL_IP",           // Nginx
                "HTTP_X_CLIENT_IP",         // Apache
                "HTTP_X_CLUSTER_CLIENT_IP", // Cluster environments
                "HTTP_FORWARDED_FOR",       // Alternative
                "HTTP_FORWARDED"            // RFC 7239
            };

            foreach (var headerName in headerNames)
            {
                var headerValue = request.ServerVariables[headerName];
                if (!string.IsNullOrEmpty(headerValue))
                {
                    var ip = ExtractFirstValidIp(headerValue);
                    if (!string.IsNullOrEmpty(ip))
                        return ip;
                }
            }

            // Also try direct headers
            var xForwardedFor = request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                var ip = ExtractFirstValidIp(xForwardedFor);
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }

            return null;
        }

        private List<string> GetProxyChainFramework(HttpRequest request)
        {
            var chain = new List<string>();

            var xForwardedFor = request.Headers["X-Forwarded-For"] ?? request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                var ips = xForwardedFor.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                chain.AddRange(ips.Select(ip => ip.Trim()));
            }

            return chain;
        }
        public string GetClientIpAddress(HttpRequest request)
        {
            var connectionInfo = GetClientConnectionInfo(request);
            return connectionInfo.IpAddress;
        }

        public string GetClientPort(HttpRequest request)
        {
            return request.ServerVariables["REMOTE_PORT"] ?? "Unknown";
        }
        //public string GetIpAddress(HttpRequest request)
        //{
        //    var clientIp = string.Empty;
        //    if (System.Web.HttpContext.Current != null)
        //    {
        //        var Request = System.Web.HttpContext.Current.Request;


        //        clientIp = Request.Headers["X-Forwarded-For"];
        //        if (string.IsNullOrEmpty(clientIp))
        //        {
        //            clientIp = Request.UserHostAddress;
        //        }
        //        else
        //        {
        //            clientIp = clientIp.Split(',').First().Trim();
        //        }

        //        // Strip out the port number if present (e.g., "84.82.19.139:36408")
        //        if (clientIp.Contains(":") && clientIp.Count(c => c == ':') == 1) // Ensure it's an IPv4 with port, not IPv6
        //        {
        //            clientIp = clientIp.Split(':')[0];
        //        }

        //    }
        //    return clientIp;

        //}
        //public string GetRemotePort(HttpRequest request)
        //{
        //    return request.ServerVariables["REMOTE_PORT"];
        //}
        public string GetDeviceType(HttpRequest request)
        {
            var userAgent = request.UserAgent;

            if (string.IsNullOrWhiteSpace(userAgent))
                return "Unknown";

            var lowerUserAgent = userAgent.ToLowerInvariant();

            // Mobile devices (most specific first)
            if (lowerUserAgent.Contains("iphone"))
                return "Mobile (iPhone, iOS)";

            if (lowerUserAgent.Contains("ipod"))
                return "Mobile (iPod Touch, iOS)";

            // Android mobile detection - check for "mobile" keyword
            if (lowerUserAgent.Contains("android") && lowerUserAgent.Contains("mobile"))
                return "Mobile (Android)";

            // Windows Phone
            if (lowerUserAgent.Contains("windows phone") || lowerUserAgent.Contains("iemobile"))
                return "Mobile (Windows Phone)";

            // BlackBerry
            if (lowerUserAgent.Contains("blackberry") || lowerUserAgent.Contains("bb10"))
                return "Mobile (BlackBerry)";

            // Generic mobile indicators
            if (lowerUserAgent.Contains("mobile") &&
                (lowerUserAgent.Contains("safari") || lowerUserAgent.Contains("chrome")))
                return "Mobile (Generic)";

            // Tablets (after mobile detection)
            if (lowerUserAgent.Contains("ipad"))
                return "Tablet (iPad, iOS)";

            // Android tablets (Android without "mobile")
            if (lowerUserAgent.Contains("android"))
                return "Tablet (Android)";

            // Surface tablets
            if (lowerUserAgent.Contains("windows") && lowerUserAgent.Contains("touch"))
                return "Tablet (Windows)";

            // Kindle
            if (lowerUserAgent.Contains("kindle") || lowerUserAgent.Contains("silk"))
                return "Tablet (Kindle)";

            // Desktop operating systems
            if (lowerUserAgent.Contains("windows nt"))
                return "Desktop (Windows)";

            if (lowerUserAgent.Contains("mac os x") && !lowerUserAgent.Contains("mobile"))
                return "Desktop (macOS)";

            if (lowerUserAgent.Contains("linux") && !lowerUserAgent.Contains("android"))
                return "Desktop (Linux)";

            if (lowerUserAgent.Contains("chromeos"))
                return "Desktop (Chrome OS)";

            // Smart TV and gaming consoles
            if (lowerUserAgent.Contains("smart-tv") || lowerUserAgent.Contains("smarttv") ||
                lowerUserAgent.Contains("googletv") || lowerUserAgent.Contains("appletv"))
                return "Smart TV";

            if (lowerUserAgent.Contains("playstation") || lowerUserAgent.Contains("xbox") ||
                lowerUserAgent.Contains("nintendo"))
                return "Gaming Console";

            // Bots and crawlers
            if (lowerUserAgent.Contains("bot") || lowerUserAgent.Contains("crawler") ||
                lowerUserAgent.Contains("spider") || lowerUserAgent.Contains("crawl"))
                return "Bot/Crawler";

            return "Other";
        }
#endif

#if NET9_0_OR_GREATER
        public async Task<int?> GetEventTypeIdByGuid(string recordGuid, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT TOP 1 ResultEventTypeId
                               FROM [dbo].[ResultEventType]
                               WHERE RecordGuid = @RecordGuid
                               """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("RecordGuid", recordGuid);
            var repo = _readRepository.GetRepository<object>();
            var result = await repo.GetSingleAsync<int?>(sql, queryParameters, null, cancellationToken, QueryType.Text);
            return result;
        }
#endif

    }
}
