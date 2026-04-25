using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.SetPrimaryClub;

public class SetPrimaryClubHandler : IRequestHandler<SetPrimaryClubCommand, OperationResultDto>
{
    private readonly IWriteRepositoryFactory _writeRepoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;
    private readonly IReadRepositoryFactory _readRepository;
    public SetPrimaryClubHandler(
        IWriteRepositoryFactory writeRepoFactory,
        IUnitOfWork unitOfWork,
        IUtilityService utilityService,
        IReadRepositoryFactory readRepository)
    {
        _writeRepoFactory = writeRepoFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
        _readRepository = readRepository;
    }

    public async Task<OperationResultDto> Handle(SetPrimaryClubCommand request, CancellationToken cancellationToken)
    {
        int userId = await _utilityService.GetCurrentUserId(cancellationToken);
        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            int memberDocId = Convert.ToInt32(
                await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(
                    "select docId from Document where SyncGuid=@MemberGuid",
                    cancellationToken,
                    new { MemberGuid = request.MemberSyncGuid.ToString() },
                    null,
                    "text"
                )
            );

            //Check MakePrimaryClub exists
            bool hasMakePrimaryClub = await ProcedureExistsAsync("MakePrimaryClub", cancellationToken);
            if (hasMakePrimaryClub)
            {
                var makePrimaryParams = new DynamicParameters();
                makePrimaryParams.Add("@ClubMemberDocId", request.ClubMemberId);
                makePrimaryParams.Add("@MemberDocId", memberDocId);
                makePrimaryParams.Add("@ActionUserId", userId);

                await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync("MakePrimaryClub", cancellationToken, makePrimaryParams, transaction);
            }
            else
            {
                // Fallback logic
                await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync(
                    @"UPDATE ClubMembers_Default SET IsPrimary = 1 WHERE DocId = @ClubMemberDocId",
                    cancellationToken,
                    new { ClubMemberDocId = request.ClubMemberId },
                    transaction, "text"
                );

                await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync(
                    @"UPDATE ClubMembers_Default
                      SET IsPrimary = 0
                      WHERE DocId IN (
                          SELECT EntityId
                          FROM Members_Links
                          WHERE EntityParentId = 3
                            AND EntityId <> @ClubMemberDocId
                            AND DocId = @MemberDocId
                      )",
                    cancellationToken,
                    new { ClubMemberDocId = request.ClubMemberId, MemberDocId = memberDocId },
                    transaction, "text"
                );
            }

            //Optional SetUserCurrency procedure
            bool hasSetUserCurrency = await ProcedureExistsAsync("SetUserCurrency", cancellationToken);
            if (hasSetUserCurrency)
            {
                await _writeRepoFactory.GetLazyRepository<object>().Value.ExecuteAsync("SetUserCurrency", cancellationToken, new { ClubMemberDocId = request.ClubMemberId }, transaction);
            }

            await _unitOfWork.CommitAsync(transaction);

            return new OperationResultDto
            {
                IsSuccess = true,
                Message = "Set primary request was processed successfully."
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    private async Task<bool> ProcedureExistsAsync(string procedureName, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT COUNT(1) as IsProced FROM sys.procedures WHERE name = @ProcName";

        var docIdParams = new DynamicParameters();
        docIdParams.Add("@ProcName", procedureName);

        var result = Convert.ToInt32(
            await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(
                sql,
                cancellationToken,
                docIdParams,
                null,
                "text"
            )
        );

        return result > 0;
    }

}
