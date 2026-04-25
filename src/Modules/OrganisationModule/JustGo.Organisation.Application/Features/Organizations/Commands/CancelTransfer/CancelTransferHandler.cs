using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using System.Data;


namespace JustGo.Organisation.Application.Features.Organizations.Commands.CancelTransfer;

public class CancelTransferHandler : IRequestHandler<CancelTransferCommand, OperationResultDto>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public CancelTransferHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<OperationResultDto> Handle(CancelTransferCommand request, CancellationToken cancellationToken)
    {
        int userId = await _utilityService.GetCurrentUserId(cancellationToken);
        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            const string sql = """
             DECLARE @LogDate DATETIME = GETUTCDATE();
             DECLARE @DeletedStateId INT = (
                 SELECT TOP 1 s.StateId
                 FROM Process p
                 INNER JOIN Repository r ON r.RepositoryId = p.PrimaryRepositoryId
                 INNER JOIN [State] s ON s.ProcessId = p.ProcessId
                 WHERE r.[Name] = 'Transfers' AND s.[Name] = 'Deleted'
             );
             DECLARE @TransferDocId INT = (
                 SELECT TOP 1 d.DocId
                 FROM Transfers_Default td
                 INNER JOIN Document d ON d.DocId = td.DocId
                 WHERE d.SyncGuid = @TransferSyncId
             );
             
             DELETE FROM Transfers_Links WHERE DocId = @TransferDocId;
             DELETE FROM Members_Links WHERE EntityId = @TransferDocId;
             
             UPDATE ProcessInfo
             SET PreviousStateId = CurrentStateId,
                 CurrentStateId = @DeletedStateId,
                 LastActionId = 0,
                 LastActionDate = @LogDate,
                 Info = 'Successfully Executed /Cancel'
             WHERE PrimaryDocId = @TransferDocId;
             
             INSERT INTO ProcessLog(InstanceId, CurrentStateId, PreviousStateId, ActionId, ActionUser, LogDate, Info)
             SELECT InstanceId, CurrentStateId, PreviousStateId, LastActionId, @ActionUserId, @LogDate, Info
             FROM ProcessInfo
             WHERE PrimaryDocId = @TransferDocId;
             """;

            var repo = _writeRepositoryFactory.GetLazyRepository<OperationResultDto>().Value;


            var parameters = new DynamicParameters();
            parameters.Add("@TransferSyncId", request.Id, DbType.Guid);
            parameters.Add("@ActionUserId", userId , DbType.Int32);
            var insertedId = await repo.ExecuteAsync(sql, cancellationToken, parameters, transaction, "Text");
            await _unitOfWork.CommitAsync(transaction);

            return new OperationResultDto
            {
                IsSuccess = true,
                Message = "Transfer cancelled successfully."
            };
        }
        catch (Exception ex)
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "Failed to cancel transfer.",
            };
        }
    }
}