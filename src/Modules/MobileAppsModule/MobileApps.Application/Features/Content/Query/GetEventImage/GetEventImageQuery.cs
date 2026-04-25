using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc;

namespace MobileApps.Application.Features.Content.Query.GetEventImage 
{
    public class GetEventImageQuery:IRequest<FileContentResult>
    {
        public long DocId { get; set; } = 1;    
        public string ImagePath { get; set; }
        public string Location { get; set; } = "";
    }
}
