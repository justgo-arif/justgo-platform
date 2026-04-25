using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc;

namespace MobileApps.Application.Features.Content.Query.GetClassImage
{
    public class GetClassImageQuery:IRequest<FileContentResult>
    {
        public long ClassId { get; set; } = 1;    
        public int EntityTypeId { get; set; }
        public string Location { get; set; } = "";
        public string StorePath { get; set; } = ""; 
    }
}
