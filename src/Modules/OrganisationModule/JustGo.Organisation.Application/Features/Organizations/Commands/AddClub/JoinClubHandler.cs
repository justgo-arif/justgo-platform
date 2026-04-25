using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.AddClub;

public class JoinClubHandler : IRequestHandler<JoinClubCommand, OperationResultDto>
{
    private readonly IWriteRepositoryFactory _writeRepoFactory;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public JoinClubHandler(IReadRepositoryFactory readRepoFactory, IWriteRepositoryFactory writeRepoFactory, IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork,
        IMediator mediator, IUtilityService utilityService)
    {
        _readRepository = readRepoFactory;
        _writeRepoFactory = writeRepoFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<OperationResultDto> Handle(JoinClubCommand request, CancellationToken cancellationToken)
    {
        int userId = await _utilityService.GetCurrentUserId(cancellationToken);
        var (clubDocId, memberDocId) = await GetDocIdFromSyncguid(request, cancellationToken);

        await JoinClubAsync(request, cancellationToken, userId, clubDocId, memberDocId);

        return new OperationResultDto
        {
            IsSuccess = true,
            Message = "Member joined successfully.",
            RowsAffected = 0
        };
    }

    private async Task<(int, int)> GetDocIdFromSyncguid(JoinClubCommand request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("ClubGuid", request.ClubGuid.ToString());
        queryParameters.Add("MemberGuid", request.MemberGuid.ToString());
        queryParameters.Add("ClubDocId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        queryParameters.Add("MemberDocId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await _readRepository.GetLazyRepository<object>().Value.GetSingleAsync(
            "GetMemberClubDocIdBySyncGuid",
            cancellationToken,
            queryParameters,
            null
        );

        int clubDocId = queryParameters.Get<int>("ClubDocId");
        int memberDocId = queryParameters.Get<int>("MemberDocId");
        return (clubDocId, memberDocId);
    }

    private async Task<bool> JoinClubAsync(JoinClubCommand request, CancellationToken cancellationToken, int userId, int clubDocId, int memberDocId)
    {
        string userGroupId = "24";
        string userRoleIds = "10,13,27";
        string clubMembershipCategory = "Member";
        DateTime clubMembershipExpiry = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        if (request.ClubMemberRoles?.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >= 0)
            userGroupId = "24,26";

        using var transaction = await _unitOfWork.BeginTransactionAsync();
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ClubDocId", clubDocId);
        queryParameters.Add("@MemberDocId", memberDocId);
        queryParameters.Add("@UserGroupIds", userGroupId);
        queryParameters.Add("@UserRoleIds", userRoleIds);
        queryParameters.Add("@ClubMemberRoles", request.ClubMemberRoles);
        queryParameters.Add("@ClubMembershipCategory", clubMembershipCategory);
        queryParameters.Add("@ClubMembershipExpiry", clubMembershipExpiry);
        queryParameters.Add("@UserId", userId);

        await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync("SaveAndLinkClubMember", cancellationToken, queryParameters, transaction);

        await _unitOfWork.CommitAsync(transaction);
        return true;
    }

}
