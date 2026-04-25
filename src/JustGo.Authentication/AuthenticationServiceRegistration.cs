using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Infrastructure.CustomAuthorizations;
using JustGo.Authentication.Infrastructure.CustomErrors;
using JustGo.Authentication.Infrastructure.Files;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Notes;
using JustGo.Authentication.Infrastructure.SystemSettings;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.Files;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomAuthorizations;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
#if NET9_0_OR_GREATER
using JustGo.Authentication.Infrastructure.FileSystemManager.AzureBlob;
using JustGo.Authentication.Infrastructure.HostedServices;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR;
#endif


namespace JustGo.Authentication
{
    public static class AuthenticationServiceRegistration
    {
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
        {
            // Disable automatic claim mapping
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddTransient<IDatabaseProvider, DatabaseProvider>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient(typeof(IReadRepository<>), typeof(ReadRepository<>));
            services.AddTransient(typeof(IWriteRepository<>), typeof(WriteRepository<>));
            services.AddTransient(typeof(LazyService<>), typeof(LazyService<>));
            services.AddTransient<IReadRepositoryFactory, ReadRepositoryFactory>();
            services.AddTransient<IWriteRepositoryFactory, WriteRepositoryFactory>();
            services.AddTransient<IUtilityService, UtilityService>();
            services.AddTransient<IJwtAthenticationService, JwtAthenticationService>();
            services.AddTransient<ISystemSettingsService, SystemSettingsService>();
            services.AddTransient<IAbacPolicyService, AbacPolicyService>();
            services.AddTransient<IAbacPolicyExtensionService, AbacPolicyExtensionService>();
#if NET9_0_OR_GREATER
            services.AddTransient<IAbacPolicyEvaluatorService, AbacPolicyEvaluatorService>();
            services.AddTransient<INoteService, NoteService>();
            services.AddTransient<IAttachmentService, AttachmentService>();
            services.AddTransient<IShortCircuitResponder, ShortCircuitResponder>();
            //Caching
            services.AddMemoryCache();
            services.AddSingleton<IRedisConnectionStringProvider, RedisConnectionStringProvider>();
            services.AddSingleton<IFusionHybridCacheProvider, FusionHybridCacheProvider>();
            services.AddTransient<IHybridCacheService, HybridCacheService>();

            services.AddScoped<IAzureBlobFileService, AzureBlobFileService>();
            services.AddScoped<IProfilePictureService, ProfilePictureService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();   
            services.AddSingleton<IProgressTrackingService, ProgressTrackingService>();
#endif      
            services.AddSingleton<ICustomError, CustomError>();
            services.AddSingleton<IJweTokenService, JweTokenService>();

            //for mobile use

            services.AddTransient<ICryptoService, CryptoService>();
            return services;
        }
    }
}
