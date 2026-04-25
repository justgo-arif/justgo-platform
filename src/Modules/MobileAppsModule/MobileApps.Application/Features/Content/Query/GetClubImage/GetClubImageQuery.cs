using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc;

namespace MobileApps.Application.Features.Content.Query.GetClubImage
{
    public class GetClubImageQuery: IRequest<FileContentResult>
    {
        public long DocId { get; set; }
        public string ImagePath { get; set; } = "club-default.png"; 
        public string Location { get; set; }="";
    }
}
