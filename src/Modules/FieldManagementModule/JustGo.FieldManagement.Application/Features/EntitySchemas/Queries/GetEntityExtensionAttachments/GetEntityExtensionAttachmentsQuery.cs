using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionAttachments
{
    public class GetEntityExtensionAttachmentsQuery : IRequest<List<IDictionary<string, object>>>
    {
        public GetEntityExtensionAttachmentsQuery(string mode, string extensionArea, int fieldId, int docId)
        {
            Mode = mode;
            ExtensionArea = extensionArea;
            FieldId = fieldId;
            DocId = docId;
        }

        public string Mode { get; set; }
        public string ExtensionArea { get; set; }
        public int FieldId { get; set; }
        public int DocId { get; set; }
    }
}
