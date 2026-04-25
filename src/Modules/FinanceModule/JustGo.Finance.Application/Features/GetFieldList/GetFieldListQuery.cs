using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.GetFieldList
{
    public class GetFieldListQuery : IRequest<List<LookupIntDto>>
    {
        public GetFieldListQuery(int fieldSetId)
        {
            FieldSetId = fieldSetId;
        }

        public int FieldSetId { get; set; }
    }

}
