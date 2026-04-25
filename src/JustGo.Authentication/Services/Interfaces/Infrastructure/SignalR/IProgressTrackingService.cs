#if NET9_0_OR_GREATER
namespace JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR
{
    public interface IProgressTrackingService
    {
        Task SendProgressAsync(string operationId, string message, int? percentage, bool isSuccess = true,
            CancellationToken cancellationToken = default);

        Task StartPeriodicProgressAsync(CancellationToken cancellationToken = default);

        void StopPeriodicProgress();
    }
}
#endif