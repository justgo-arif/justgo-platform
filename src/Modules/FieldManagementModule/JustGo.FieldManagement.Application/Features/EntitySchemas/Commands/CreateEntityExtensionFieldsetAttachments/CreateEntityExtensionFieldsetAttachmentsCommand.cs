using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.CreateEntityExtensionFieldsetAttachments
{
    public class CreateEntityExtensionFieldsetAttachmentsCommand : IRequest<int>
    {
        public List<int> FieldIds { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
