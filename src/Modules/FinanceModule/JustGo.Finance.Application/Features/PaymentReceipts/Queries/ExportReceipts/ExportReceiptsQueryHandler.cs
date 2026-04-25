using System.Globalization;
using CsvHelper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.ExportDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.ExportReceipts
{
    public class ExportReceiptsQueryHandler : IRequestHandler<ExportReceiptsQuery, ExportResultDto>
    {
        private readonly IMediator _mediator;
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IAzureBlobFileService _azureBlobFileService;
        public ExportReceiptsQueryHandler(IMediator mediator, LazyService<IReadRepository<dynamic>> readRepository, IUtilityService utilityService
            , IAzureBlobFileService azureBlobFileService)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _azureBlobFileService = azureBlobFileService;
        }

        public async Task<ExportResultDto> Handle(ExportReceiptsQuery request, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = $"payment_receipts_{timestamp}.csv";
            var destinationPath = await _azureBlobFileService.MapPath($"~/store/financeattachments/{fileName}");

            await using var memory = new MemoryStream();
            await using var writer = new StreamWriter(memory);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.Context.RegisterClassMap<PaymentInfoMap>();
            csv.WriteHeader<ExportedReceiptDto>();
            await csv.NextRecordAsync();

            int pageNo = 1;
            int pageSize = 10000;

            while (true)
            {
                var query = new ExportReceiptsPagedQuery(request.MerchantId, request.FromDate, request.ToDate,
                    request.PaymentIds, request.PaymentMethods, request.StatusIds, request.ScopeKey,
                    request.SearchText, request.ColumnName, request.OrderBy,
                    pageNo, pageSize);

                var records = await _mediator.Send(query, cancellationToken);
                if (records == null || !records.Any())
                    break;

                await csv.WriteRecordsAsync(records, cancellationToken);
                pageNo++;
            }
            await writer.FlushAsync();
            memory.Position = 0;
            var fileBytes = memory.ToArray();
            await _azureBlobFileService.UploadFileAsync(destinationPath, fileBytes, FileMode.Create);
            var downloadUrl = $"/store/download?f={fileName}&t=finance&p=-1&p1=-1&p2=-1&p3=-1";
            return new ExportResultDto
            {
                FileName = fileName,
                DownloadUrl = downloadUrl
            };
        }

    }
}
