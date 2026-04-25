#if NET9_0_OR_GREATER
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR.RealTimeProgress;
using Microsoft.AspNetCore.SignalR;


namespace JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR
{
    public class ProgressTrackingService : IProgressTrackingService
    {
        private readonly IHubContext<ProgressTrackingHub> _hubContext;
        private Timer? _progressTimer;
        private string? _currentOperationId;
        private string? _currentMessage;
        private int? _currentProgress;
        private int _currentStep;
        private bool _isSuccess;

        public ProgressTrackingService(IHubContext<ProgressTrackingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendProgressAsync(string operationId, string message, int? percentage, bool isSuccess = true,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(operationId))
            {
                await _hubContext.Clients.Group(operationId).SendAsync("ReceiveProgress", new
                {
                    OperationId = operationId,
                    IsSuccess = isSuccess,
                    Message = message,
                    Percentage = percentage,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);
            }

            _currentMessage = message;
            _currentProgress = percentage;
            _currentOperationId = operationId;
            _isSuccess = isSuccess;
        }

        public Task StartPeriodicProgressAsync(
            CancellationToken cancellationToken = default)
        {
            _currentStep = 0;

            _progressTimer = new Timer(async void (_) =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested || string.IsNullOrEmpty(_currentOperationId)) return;

                    _currentMessage = _currentMessage?.TrimEnd('.');

                    _currentStep = (_currentStep % 4) + 1;
                    var dots = new string('.', _currentStep);
                    var message = $"{_currentMessage}{dots}";
                    await SendProgressAsync(_currentOperationId, message, _currentProgress, _isSuccess,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Progress update failed: {ex.Message}");
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        public void StopPeriodicProgress()
        {
            _progressTimer = null;
            _currentOperationId = null;
            _currentMessage = null;
        }
    }
}
#endif