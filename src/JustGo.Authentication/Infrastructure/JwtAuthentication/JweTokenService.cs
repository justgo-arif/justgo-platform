using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
    public class JweTokenService: IJweTokenService
    {
        private readonly IReadRepositoryFactory _readRepository;

        public JweTokenService(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public string GenerateEncryptedAccessToken(JwtTokenParameter tokenParameter)
        {
            try
            {
                var issuedAt = DateTime.UtcNow;
                var tokenExpiryTimeStamp = issuedAt.AddMinutes(tokenParameter.ExpiryMinutes);

                // Create signing credentials for the inner JWT
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenParameter.SecretKey));
                var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

                // Create encryption credentials for the outer JWE
                var encryptionKeyString = tokenParameter.EncryptionKey ?? tokenParameter.SecretKey;
                var encryptionKeyBytes = DeriveKey(encryptionKeyString, 32); // Ensure 256-bit key for A256KW

                var encryptionKey = new SymmetricSecurityKey(encryptionKeyBytes)
                {
                    KeyId = tokenParameter.TenantClientId
                };
                var encryptingCredentials = new EncryptingCredentials(encryptionKey, tokenParameter.KeyEncryptionAlgorithm, tokenParameter.ContentEncryptionAlgorithm);

                var claims = new List<Claim>
                {
                    new Claim("UniqueGuid", Guid.NewGuid().ToString()),
                    new Claim("UserSyncId", tokenParameter.UserSyncId.ToString()),
                    new Claim("DateOfBirth", tokenParameter.DateOfBirth.ToString()),
                    new Claim("TenantGuid", tokenParameter.TenantGuid.ToString()),
                    new Claim("TenantClientId", tokenParameter.TenantClientId)
                };

                foreach (var role in tokenParameter.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

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

                foreach (var familyMember in tokenParameter.FamilyMembers)
                {
                    claims.Add(new Claim("family_members", familyMember));
                }

                var claimsIdentity = new ClaimsIdentity(claims);

                var securityTokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claimsIdentity,
                    Issuer = tokenParameter.Issuer,
                    Audience = tokenParameter.Audience,
                    IssuedAt = issuedAt,
                    Expires = tokenExpiryTimeStamp,
                    SigningCredentials = signingCredentials,
                    EncryptingCredentials = encryptingCredentials // This makes it a JWE token
                };

                var tokenHandler = new JsonWebTokenHandler();
                var encryptedToken = tokenHandler.CreateToken(securityTokenDescriptor);

                return encryptedToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating encrypted access token: {ex.Message}", ex);
            }
        }
        public ClaimsPrincipal? GetClaimsPrincipalFromEncryptedToken(string jweToken)
        {
            jweToken = CleanToken(jweToken);
            var tenant = GetTenantFromEncryptedToken(jweToken);
            if (tenant is null)
            {
                return null;
            }

            // Ensure proper key size for decryption
            var encryptionKeyString = tenant.JwtAccessTokenEncryptionKey ?? tenant.JwtAccessTokenSecretKey;
            var encryptionKeyBytes = DeriveKey(encryptionKeyString, 32);

            var encryptionKey = new SymmetricSecurityKey(encryptionKeyBytes);
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tenant.JwtAccessTokenSecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = tenant.ApiUrl,
                ValidateAudience = true,
                ValidAudience = tenant.TenantDomainUrl,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = signingKey,
                TokenDecryptionKey = encryptionKey // Key for decryption
            };

            var tokenHandler = new JsonWebTokenHandler();
            var result = tokenHandler.ValidateTokenAsync(jweToken, validationParameters).GetAwaiter().GetResult();
            if (result.IsValid)
            {
                return new ClaimsPrincipal(result.ClaimsIdentity);
            }

            return null;
        }

        private byte[] DeriveKey(string keyString, int keyLength)
        {
            using (var sha256 = SHA256.Create())
            {
                var keyBytes = Encoding.UTF8.GetBytes(keyString);
                var hashedKey = sha256.ComputeHash(keyBytes);

                // If we need a longer key, concatenate hash with itself
                if (keyLength > hashedKey.Length)
                {
                    var extendedKey = new byte[keyLength];
                    var hashLength = hashedKey.Length;

                    for (int i = 0; i < keyLength; i++)
                    {
                        extendedKey[i] = hashedKey[i % hashLength];
                    }

                    return extendedKey;
                }

                // If we need a shorter key or exact length, truncate
                var derivedKey = new byte[keyLength];
                Array.Copy(hashedKey, 0, derivedKey, 0, Math.Min(keyLength, hashedKey.Length));

                return derivedKey;
            }
        }
        private dynamic? GetTenantFromEncryptedToken(string jweToken)
        {
            var tokenHandler = new JsonWebTokenHandler();
            var jsonToken = tokenHandler.ReadJsonWebToken(jweToken);

            if (jsonToken.TryGetHeaderValue<string>("kid", out var tenantClientId))
            {
                return GetTenantByTenantClientId(tenantClientId);
            }
            return null;
        }
        public string? GetClaimFromTokenByType(string token, string claimType)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            token = CleanToken(token);
            if (IsJweToken(token)) // JWE token
            {
                var claimsPrincipal = GetClaimsPrincipalFromExpiredEncryptedToken(token);
                return claimsPrincipal?.FindFirst(claimType)?.Value;
            }
            else // JWT token
            {
                token = CleanToken(token);
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwt = jwtHandler.ReadJwtToken(token);
                return jwt.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
            }
        }
        public List<Claim>? GetClaimsFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            token = CleanToken(token);
            if (IsJweToken(token)) // JWE token
            {
                var claimsPrincipal = GetClaimsPrincipalFromExpiredEncryptedToken(token);
                return claimsPrincipal?.Claims?.ToList();
            }
            else // JWT token
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwt = jwtHandler.ReadJwtToken(token);
                return jwt.Claims.ToList();
            }
        }
        public bool IsJweToken(string token)
        {
            try
            {
                var cleanToken = CleanToken(token);
                var parts = cleanToken.Split('.');
                return parts.Length == 5; // JWE has 5 parts, JWT has 3
            }
            catch
            {
                return false;
            }
        }
        public string CleanToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return token;

            token = token.Trim();

            // Check and remove "Bearer%20" first (longer prefix)
            if (token.StartsWith("Bearer%20", StringComparison.OrdinalIgnoreCase))
                return token.Substring(9).Trim();

            // Then check and remove "Bearer " (shorter prefix)
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return token.Substring(7).Trim();

            return token;
        }

        private ClaimsPrincipal? GetClaimsPrincipalFromExpiredEncryptedToken(string jweToken)
        {
            jweToken = CleanToken(jweToken);
            var tenant = GetTenantFromEncryptedToken(jweToken);
            if (tenant is null)
            {
                return null;
            }

            // Ensure proper key size for decryption
            var encryptionKeyString = tenant.JwtAccessTokenEncryptionKey ?? tenant.JwtAccessTokenSecretKey;
            var encryptionKeyBytes = DeriveKey(encryptionKeyString, 32);

            var encryptionKey = new SymmetricSecurityKey(encryptionKeyBytes);
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tenant.JwtAccessTokenSecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = tenant.ApiUrl,
                ValidateAudience = true,
                ValidAudience = tenant.TenantDomainUrl,
                ValidateLifetime = false, // Ignore token expiration
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = signingKey,
                TokenDecryptionKey = encryptionKey // Key for decryption
            };

            var tokenHandler = new JsonWebTokenHandler();
            var result = tokenHandler.ValidateTokenAsync(jweToken, validationParameters).GetAwaiter().GetResult();
            if (result.IsValid)
            {
                return new ClaimsPrincipal(result.ClaimsIdentity);
            }

            return null;
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

    }
}
