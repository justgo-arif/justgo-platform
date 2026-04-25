#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR.RealTimeProgress
{
    [CustomAuthorize]
    public class ProgressTrackingHub(ILogger<ProgressTrackingHub> logger) : Hub
    {
        private static readonly ConcurrentDictionary<string, DateTime> ActiveUploads = new();

        public async Task SendProgress(string uploadSessionId, object progressData)
        {
            if (string.IsNullOrWhiteSpace(uploadSessionId))
            {
                logger.LogWarning("SendProgress called with null or empty uploadSessionId");
                return;
            }

            try
            {
                await Clients.Group(uploadSessionId).SendAsync("ReceiveProgress", progressData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send progress for upload session {UploadSessionId}", uploadSessionId);
            }
        }

        public async Task JoinUploadGroup(string uploadSessionId)
        {
            if (string.IsNullOrWhiteSpace(uploadSessionId))
            {
                logger.LogWarning("JoinUploadGroup called with null or empty uploadSessionId from connection {ConnectionId}",
                    Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Invalid upload session ID");
                return;
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, uploadSessionId);
                ActiveUploads.TryAdd(uploadSessionId, DateTime.UtcNow);

                await Clients.Caller.SendAsync("JoinedGroup", uploadSessionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add connection {ConnectionId} to upload group {UploadSessionId}",
                    Context.ConnectionId, uploadSessionId);
                await Clients.Caller.SendAsync("Error", "Failed to join upload group");
            }
        }

        public async Task LeaveUploadGroup(string uploadSessionId)
        {
            if (string.IsNullOrWhiteSpace(uploadSessionId))
            {
                logger.LogWarning("LeaveUploadGroup called with null or empty uploadSessionId from connection {ConnectionId}",
                    Context.ConnectionId);
                return;
            }

            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, uploadSessionId);
                ActiveUploads.TryRemove(uploadSessionId, out _);

                await Clients.Caller.SendAsync("LeftGroup", uploadSessionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove connection {ConnectionId} from upload group {UploadSessionId}",
                    Context.ConnectionId, uploadSessionId);
            }
        }

        public async Task NotifyUploadComplete(string uploadSessionId, object result)
        {
            if (string.IsNullOrWhiteSpace(uploadSessionId))
            {
                logger.LogWarning("NotifyUploadComplete called with null or empty uploadSessionId");
                return;
            }

            try
            {
                await Clients.Group(uploadSessionId).SendAsync("UploadComplete", result);
                ActiveUploads.TryRemove(uploadSessionId, out _);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send upload completion notification for session {UploadSessionId}", uploadSessionId);
            }
        }

        public async Task NotifyUploadError(string uploadSessionId, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(uploadSessionId))
            {
                logger.LogWarning("NotifyUploadError called with null or empty uploadSessionId");
                return;
            }

            try
            {
                await Clients.Group(uploadSessionId).SendAsync("UploadError", new { error = errorMessage, timestamp = DateTime.UtcNow });
                ActiveUploads.TryRemove(uploadSessionId, out _);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send upload error notification for session {UploadSessionId}", uploadSessionId);
            }
        }

        public override async Task OnConnectedAsync()
        {
            logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            }

            CleanupConnectionUploads();
            await base.OnDisconnectedAsync(exception);
        }

        private void CleanupConnectionUploads()
        {
            try
            {
                var expiredUploads = new List<string>();
                var cutoffTime = DateTime.UtcNow.AddHours(-1); // Remove uploads older than 1 hour

                foreach (var upload in ActiveUploads)
                {
                    if (upload.Value < cutoffTime)
                    {
                        expiredUploads.Add(upload.Key);
                    }
                }

                foreach (var uploadId in expiredUploads)
                {
                    ActiveUploads.TryRemove(uploadId, out _);
                }

                if (expiredUploads.Count > 0)
                {
                    logger.LogInformation("Cleaned up {Count} expired upload sessions", expiredUploads.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during upload cleanup for connection {ConnectionId}", Context.ConnectionId);
            }
        }
    }
}
#endif