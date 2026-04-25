using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetFieldNameByFieldId
{
    public class GetFieldNameByFieldIdQuery: IRequest<string>
    {
        public int FieldId { get; }
        public GetFieldNameByFieldIdQuery(int fieldId)
        {
            FieldId = fieldId;
        }
    }
}
