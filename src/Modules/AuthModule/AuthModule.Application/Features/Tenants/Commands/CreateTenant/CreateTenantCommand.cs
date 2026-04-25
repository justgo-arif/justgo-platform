using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Commands.CreateTenant
{
    public class CreateTenantCommand : IRequest<int>
    {
        public Guid TenantGuid { get; set; }
        public string TenantName { get; set; }
        public string TenantDescription { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string DBUserId { get; set; }
        public string DBPassword { get; set; }
        public string ServerLocation { get; set; }
        public string TenantClientId { get; set; }
        public string ApiUrl { get; set; }
        public string TenantDomainUrl { get; set; }
        public string JwtAccessTokenSecretKey { get; set; }
        public int JwtAccessTokenExpiryMinutes { get; set; }
        public int JwtRefreshTokenExpiryMinutes { get; set; }
    }
}
