using AuthModule.API;
using AuthModule.API.Middlewares;
using JustGo.AssetManagement.API;
using JustGo.Authentication;
using JustGo.Authentication.Infrastructure.CustomCors;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.FileSystemManager.AzureBlob;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR.RealTimeProgress;
using JustGo.Booking.API;
using JustGo.Credential.API;
using JustGo.FieldManagement.API;
using JustGo.Finance.API;
using JustGo.MemberProfile.API;
using JustGo.Membership.API;
using JustGo.Organisation.API;
using JustGo.Result.API;
using JustGoAPI.API.ApiVersioning;
using JustGoAPI.API.SwaggerConfig;
using JustGoAPI.Shared.CustomAutoMapper;
using JustGoAPI.Shared.Helper;
using MobileApps.API;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
});

builder.Services.AddCustomCors(builder.Environment);
builder.Services.AddCustomHttpClient();
builder.Services.AddMapsterProfiles();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddApiVersioningConfiguration();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddHttpContextAccessor();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthenticationServices();
builder.Services.AddAuthServices();
builder.Services.AddMobileAuthServices();
builder.Services.AddMemberProfileServices();
builder.Services.AddOrganisationServices();
builder.Services.AddMembershipServices();
builder.Services.AddFinanceServices();
builder.Services.AddAssetManagementServices();
builder.Services.AddFieldManagementServices();
builder.Services.AddCredentialServices();
builder.Services.AddBookingServices();
builder.Services.AddResultServices();
builder.Services.AddSignalR();
builder.Services.AddJwtAuthentication();

//compression of responses
builder.Services.AddResponseCompression(options => 
{ 
    options.EnableForHttps = true;

});

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        //.WriteTo.AuditSink(services)
        .WriteTo.EventSink(services)
        .WriteTo.ExceptionSink(services);
});
builder.Host.UseDefaultServiceProvider(
       (context, options) =>
       {
           options.ValidateOnBuild = true;
           options.ValidateScopes = true;
       });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
//response compression
app.UseResponseCompression();

app.UseStaticFiles();
app.UseSwaggerDocumentation();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(CorsConfiguration.PolicyName);
//app.UseCors();
//app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<CustomAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseCustomErrorMiddleware();
app.MapControllers();
app.MapHub<ProgressTrackingHub>("/progressReportHub");

app.Run();


