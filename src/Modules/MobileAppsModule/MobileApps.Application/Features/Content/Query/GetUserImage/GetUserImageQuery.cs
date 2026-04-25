using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc;

namespace MobileApps.Application.Features.Content.Query.GetUserImage
{
    public class GetUserImageQuery:IRequest<FileContentResult>
    {
        public int UserId { get; set; } = 1;
        public string ImagePath { get; set; }
        public string Gender{ get; set; }
    }
}
