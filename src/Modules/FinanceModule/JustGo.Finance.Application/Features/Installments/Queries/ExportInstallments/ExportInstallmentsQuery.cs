using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.ExportDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments
{
    public class ExportInstallmentsQuery : DateRangeFilter, IRequest<ExportResultDto>
    {
        public GetInstallmentFilter Filter { get; }
        public RecurringType RecurringType { get; }

        public ExportInstallmentsQuery(GetInstallmentFilter filter, RecurringType recurringType)
        {
            Filter = filter;
            RecurringType = recurringType;
        }
    }
}
