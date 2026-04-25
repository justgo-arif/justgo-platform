using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail
{
    public class SendAssetEmailCommand : IRequest<bool>
    {
        public string MessageScheme { get; set; }
        public string Argument { get; set; }
        public int ForEntityId { get; set; }
        public int TypeEntityId { get; set; }
        public int InvokeUserId { get; set; }
        public int OwnerId { get; set; }

    }
}
