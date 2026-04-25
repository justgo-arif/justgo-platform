using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using System.Data;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.LeaveClub;

public class LeaveClubHandler : IRequestHandler<LeaveClubCommand, OperationResultDto<string>>
{
    private readonly IWriteRepositoryFactory _writeRepoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public LeaveClubHandler(IWriteRepositoryFactory writeRepoFactory, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepoFactory = writeRepoFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<OperationResultDto<string>> Handle(LeaveClubCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

        return await LeaveClubAsync(request, currentUserId, cancellationToken);
    }

    private async Task<OperationResultDto<string>> LeaveClubAsync(LeaveClubCommand request, int currentUserId, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Result", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
            parameters.Add("@ClubSyncGuid", request.ClubGuid);
            parameters.Add("@MemberSyncGuid", request.MemberGuid);
            parameters.Add("@InvokingUserId", currentUserId);
            parameters.Add("@ClubMemberRole", request.ClubMemberRoles);

            await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync("EXEC IsMemberValidForClubMemberAdd @ClubSyncGuid, @MemberSyncGuid, @InvokingUserId, @ClubMemberRole, @Result OUTPUT", cancellationToken, parameters, transaction, "text");

            var result = parameters.Get<string>("@Result");
            if (!string.IsNullOrWhiteSpace(result))
            {
                return new OperationResultDto<string>
                {
                    IsSuccess = false,
                    Message = "User is not authorized to perform this action.",
                    RowsAffected = 0
                };
            }

            var rejectParams = new DynamicParameters();
            rejectParams.Add("@ClubSyncGuid", request.ClubGuid);
            rejectParams.Add("@MemberSyncGuid", request.MemberGuid);
            rejectParams.Add("@Reason", request.Reason);
            rejectParams.Add("@ActionUserId", currentUserId);

            await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync("RejectClubMemberBySyncGuid", cancellationToken, rejectParams, transaction);

            await _unitOfWork.CommitAsync(transaction);

            return new OperationResultDto<string>
            {
                IsSuccess = true,
                Message = "Club member removed successfully.",
                RowsAffected = 1
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            return new OperationResultDto<string>
            {
                IsSuccess = false,
                Message = ex.Message,
                RowsAffected = 0
            };
        }
    }

}