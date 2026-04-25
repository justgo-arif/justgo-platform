using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.Notes.Commands.DeleteNoteCommands
{
    public class DeleteAssetCommand: IRequest<string>
    {
        public string AssetRegisterId {  get; set; }
    }
}
