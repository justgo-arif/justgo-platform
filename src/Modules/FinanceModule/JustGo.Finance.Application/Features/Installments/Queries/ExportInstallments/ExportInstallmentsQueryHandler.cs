using System.Globalization;
using CsvHelper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.ExportDTOs;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.DTOs.SubscriptionDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;

namespace JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments
{

    public class ExportInstallmentsQueryHandler : IRequestHandler<ExportInstallmentsQuery, ExportResultDto>
    {
        private readonly IMediator _mediator;
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IAzureBlobFileService _azureBlobFileService;
        public ExportInstallmentsQueryHandler(IMediator mediator, LazyService<IReadRepository<dynamic>> readRepository, IUtilityService utilityService
            ,IAzureBlobFileService azureBlobFileService)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _azureBlobFileService = azureBlobFileService;
        }

        public async Task<ExportResultDto> Handle(ExportInstallmentsQuery request, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            var fileName = request.RecurringType == RecurringType.Installment
                            ? $"installments_{timestamp}.csv"
                            : $"subscriptions_{timestamp}.csv";

            var destinationPath = await _azureBlobFileService.MapPath($"~/store/financeattachments/{fileName}");

            await using var memory = new MemoryStream();
            await using var writer = new StreamWriter(memory);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            switch (request.RecurringType)
            {
                case RecurringType.Installment:
                    csv.Context.RegisterClassMap<InstallmentMap>();
                    csv.WriteHeader<InstallmentDto>();
                    break;
                case RecurringType.Subscription:
                    csv.Context.RegisterClassMap<SubscriptionsMap>();
                    csv.WriteHeader<SubscriptionsDto>();
                    break;
            }
            await csv.NextRecordAsync();


            int pageNo = 1;
            int pageSize = 10000;

            while (true)
            {
                var query = new GetInstallmentPagedQuery(request.Filter.MerchantId, request.RecurringType, request.Filter.StatusIds, request.Filter.PlanIds, request.Filter.ScopeKey, request.Filter.SearchText, request.Filter.FromDate, request.Filter.ToDate, pageNo, pageSize);

                var records = await _mediator.Send(query, cancellationToken);
                if (records == null || !records.Any())
                    break;

                await csv.WriteRecordsAsync(records, cancellationToken);
                pageNo++;
            }
            await writer.FlushAsync();
            memory.Position = 0;
            var fileBytes = memory.ToArray();
            var url = await _azureBlobFileService.UploadFileAsync(destinationPath, fileBytes, FileMode.Create);
            var downloadUrl = $"/store/download?f={fileName}&t=finance&p=-1&p1=-1&p2=-1&p3=-1";
            return new ExportResultDto
            {
                FileName = fileName,
                DownloadUrl = downloadUrl
            };
        }

    }
}
