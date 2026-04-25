using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Commands.AddCompetition;

public class AddCompetitionCommandHandler : IRequestHandler<AddCompetitionCommand, Result<AddCompetitionResponse>>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;
    private readonly IAzureBlobFileService _azureBlobFileService;

    public AddCompetitionCommandHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork, IUtilityService utilityService,
            IAzureBlobFileService azureBlobFileService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
        _azureBlobFileService = azureBlobFileService;
    }

    public async Task<Result<AddCompetitionResponse>> Handle(AddCompetitionCommand request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);

        try
        {
            string competitionRecordGuid = Guid.NewGuid().ToString();
            if (!string.IsNullOrEmpty(request.ImagePath))
            {
                var sourcePath = await _azureBlobFileService.MapPath($"~/store/Temp/competitionattachment/attachments/{request.ImagePath}");
                var destinationDir = await _azureBlobFileService.MapPath($"~/store/competitionattachment/{competitionRecordGuid}");
                var destinationPath = $"{destinationDir}/{request.ImagePath}";
                await _azureBlobFileService.MoveFileAsync(sourcePath, destinationPath);
            }
            var writeRepo = _writeRepositoryFactory.GetRepository<object>();

            string insertCompetitionSql = """
                   INSERT INTO ResultEvents
                   (
                       EventName,
                       EventDocId,
                       StartDate,
                       EndDate,
                       --Reference,
                       CategoryId,
                       OwnerId,
                       ResultEventTypeId,
                       TimeZone,
                       ImagePath,
                       Postcode,
                       County,
                       Town,
                       Address1,
                       Address2,
                       RecordGuid,
                       SourceType
                   )
                   VALUES
                   (
                       @EventName,
                       0,
                       dbo.GET_LOCAL_UTC_DATE_TIME(@StartDate, @TimeZone),
                       dbo.GET_LOCAL_UTC_DATE_TIME(@EndDate, @TimeZone),
                       --@Reference,
                       @CategoryId,
                       @OwnerId,
                       @ResultEventTypeId,
                       @TimeZone,
                       @ImagePath,
                       @Postcode,
                       @County,
                       @Town,
                       @Address1,
                       @Address2,
                       @competitionRecordGuid,
                       @SourceType
                   );
                   SELECT CAST(SCOPE_IDENTITY() AS INT);
                   """;

            var parameters = new DynamicParameters();
            parameters.Add("@EventName", request.EventName);
            parameters.Add("@StartDate", request.StartDate);
            parameters.Add("@EndDate", request.EndDate);
            //parameters.Add("@Reference", request.Reference);
            parameters.Add("@CategoryId", request.CategoryId);
            parameters.Add("@OwnerId", ownerId);
            parameters.Add("@ResultEventTypeId", request.ResultEventTypeId);
            parameters.Add("@TimeZone", request.TimeZone);
            parameters.Add("@ImagePath", request.ImagePath);
            parameters.Add("@Postcode", request.Postcode);
            parameters.Add("@County", request.County);
            parameters.Add("@Town", request.Town);
            parameters.Add("@Address1", request.Address1);
            parameters.Add("@Address2", request.Address2);
            parameters.Add("@competitionRecordGuid", competitionRecordGuid);
            parameters.Add("@SourceType", CompetitionSourceType.Manual);

            var eventId = await writeRepo.ExecuteScalarAsync<int>(insertCompetitionSql, cancellationToken, parameters, transaction, QueryType.Text);

            await transaction.CommitAsync(cancellationToken);

            var response = new AddCompetitionResponse
            {
                EventId = eventId,
                IsSuccess = true,
                Message = "Event competition added successfully."
            };

            CustomLog.Event(
                AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultView.Value,
                AuditScheme.ResultManagement.ResultView.Inserted.Value,
                0,
                eventId,
                EntityType.Result,
                0,
                nameof(AuditLogSink.ActionType.Created),
                $"Event competition created successfully for EventId: {eventId}"
            );

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<AddCompetitionResponse>.Failure("Failed to add event competition.", ErrorType.BadRequest);
        }
    }
}