using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.Organisation.Application.Features.Organizations.Commands.ClubTransferRequest;
using System.Data;

namespace JustGo.Organisation.Application.Features.Transfers.Handlers;

public class ClubTransferRequestHandler : IRequestHandler<ClubTransferRequestCommand, OperationResultDto<int>>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public ClubTransferRequestHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<OperationResultDto<int>> Handle(ClubTransferRequestCommand request, CancellationToken cancellationToken)
    {
        int userId = await _utilityService.GetCurrentUserId(cancellationToken);

        return await GetClubIdByUserId(request, userId, cancellationToken);
    }

    private async Task<OperationResultDto<int>> GetClubIdByUserId(ClubTransferRequestCommand request, int userId, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var queryParams = new DynamicParameters();
            queryParams.Add("@ClubSyncGuid", request.FromClubSyncGuid);
            queryParams.Add("@MemberSyncGuid", request.MemberSyncGuid);
            queryParams.Add("@InvokingUserId", userId);
            queryParams.Add("@ClubMemberRole", "Member");
            queryParams.Add("@Result", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

            await _writeRepositoryFactory.GetLazyRepository<object>().Value.ExecuteAsync("IsMemberValidForClubMemberAdd", cancellationToken, queryParams);

            if (!string.IsNullOrWhiteSpace(queryParams.Get<string>("@Result")))
            {
                return new OperationResultDto<int>
                {
                    IsSuccess = false,
                    Message = queryParams.Get<string>("@Result"),
                    Data = 0
                };
            }

            var transferParams = new DynamicParameters();
            transferParams.Add("@memberSyncGuid", request.MemberSyncGuid);
            transferParams.Add("@fromClubSyncGuid", request.FromClubSyncGuid);
            transferParams.Add("@toClubSyncGuid", request.ToClubSyncGuid);
            transferParams.Add("@reasonForMove", request.ReasonForMove);
            transferParams.Add("@userId", userId);
            transferParams.Add("@trDocId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _writeRepositoryFactory.GetLazyRepository<object>().Value.ExecuteAsync("CreateTransferDocumentBySyncGuid", cancellationToken, transferParams);

            await _unitOfWork.CommitAsync(transaction);

            return new OperationResultDto<int>
            {
                IsSuccess = true,
                Message = "Club transfer request was processed successfully.",
                Data = transferParams.Get<int>("@trDocId")
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            return new OperationResultDto<int>
            {
                IsSuccess = false,
                Message = ex.Message,
                Data = 0
            };
        }

    }
}
