using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.FamilyActionToken
{
    public class FamilyActionTokenHandler : IRequestHandler<FamilyActionTokenQuery, ActionTokenHandlerResponse>
    {
        private readonly IWriteRepositoryFactory _writeRepoFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IReadRepositoryFactory _readRepositoryFactory;

        public FamilyActionTokenHandler(
            IWriteRepositoryFactory writeRepoFactory,
            IUnitOfWork unitOfWork,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService, IReadRepositoryFactory readRepositoryFactory)
        {
            _writeRepoFactory = writeRepoFactory;
            _unitOfWork = unitOfWork;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _readRepositoryFactory = readRepositoryFactory;
        }

        public async Task<ActionTokenHandlerResponse> Handle(FamilyActionTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var actionToken = await Select(request.Token, cancellationToken);
                if (actionToken == null)
                    return ErrorResponse(request.Token, "No Token found");

                if (actionToken.InvokeAttempts > 1)
                    return ErrorResponse(actionToken.Token, "Token has already been invoked once");

                if (actionToken.Status == ActionTokenStatus.Executed)
                    return ErrorResponse(actionToken.Token, "Token already executed");

                if (actionToken.Status == ActionTokenStatus.Expired || actionToken.Status == ActionTokenStatus.Error)
                    return ErrorResponse(actionToken.Token, "Token has expired");

                if (actionToken.VaildFor < (DateTime.UtcNow - actionToken.CreateDate).TotalMinutes)
                {
                    await SetStatus(request.Token, ActionTokenStatus.Expired, cancellationToken);
                    return ErrorResponse(actionToken.Token, "Token has expired");
                }

                await SetStatus(request.Token, ActionTokenStatus.InProgress, cancellationToken);

                if (actionToken.HandlerArguments == null ||
                    !actionToken.HandlerArguments.TryGetValue("FamilyDocId", out var familyDocIdStr) ||
                    !actionToken.HandlerArguments.TryGetValue("TargetMemberDocId", out var targetMemberDocIdStr) ||
                    !actionToken.HandlerArguments.TryGetValue("RedirectUrl", out var redirectUrl))
                {
                    return ErrorResponse(actionToken.Token, "Required Handler Arguments not found");
                }

                if (!int.TryParse(familyDocIdStr, out var familyDocId) ||
                    !int.TryParse(targetMemberDocIdStr, out var targetMemberDocId))
                {
                    return ErrorResponse(actionToken.Token, "Invalid Handler Argument values");
                }

                var addFamilyMember = await AddMemberToFamily(familyDocId, targetMemberDocId, cancellationToken);

                await SetStatus(request.Token, addFamilyMember ? ActionTokenStatus.Executed : ActionTokenStatus.Error, cancellationToken);
                await IncreaseInvokeAttemptCount(request.Token, cancellationToken);

                return new ActionTokenHandlerResponse
                {
                    Token = actionToken.Token,
                    Success = addFamilyMember,
                    ErrorMessage = "",
                    RedirectUrl = redirectUrl
                };
            }
            catch (Exception)
            {
                return ErrorResponse(request.Token, "An unexpected error occurred.");
            }
        }

        private ActionTokenHandlerResponse ErrorResponse(string token, string message) =>
            new ActionTokenHandlerResponse { Token = token, Success = false, ErrorMessage = message, RedirectUrl = "" };

        public async Task<bool> AddMemberToFamily(int familyDocId, int targetMemberDocId, CancellationToken cancellationtoken)
        {

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            var readRepo = _readRepository.GetLazyRepository<object>().Value;

            string query = @"
                            DECLARE @FamilyRepoId INT = (SELECT RepositoryId FROM Repository WHERE Name = 'Family');
                            DECLARE @MemberRepoId INT = 1;

                            BEGIN TRY
                                BEGIN TRANSACTION

                                INSERT INTO Family_Links (DocID, Entityparentid, Entityid, Title)
                                VALUES (@familyDocId, @MemberRepoId, @targetMemberDocId, 'Family-Member Link');

                                INSERT INTO Members_Links (DocID, Entityparentid, Entityid, Title)
                                VALUES (@targetMemberDocId, @FamilyRepoId, @familyDocId, 'Member-Family Link');

                                COMMIT;

                                SELECT 1;
                            END TRY
                            BEGIN CATCH
                                ROLLBACK;
                                DECLARE @Err NVARCHAR(MAX) = ERROR_MESSAGE();
                                RAISERROR(@Err, 16, 1);
                                SELECT 0;
                            END CATCH
                        ";

            var parameters = new
            {
                familyDocId = familyDocId,
                targetMemberDocId = targetMemberDocId
            };

            var result = await readRepo.GetSingleAsync(query, cancellationtoken, parameters, transaction, "text");
            await _unitOfWork.CommitAsync(transaction);
            return Convert.ToInt32(result) == 1;
        }

        public async Task IncreaseInvokeAttemptCount(string token, CancellationToken cancellationToken)
        {
            var repo = _writeRepoFactory.GetLazyRepository<JustGo.MemberProfile.Domain.Entities.ActionToken>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            var query = "UPDATE ActionToken SET InvokeAttempts = (ISNULL(InvokeAttempts, 0) + 1) WHERE Token = @Token";
            await repo.ExecuteAsync(query, cancellationToken, new { Token = token }, transaction, "text");

            await _unitOfWork.CommitAsync(transaction);

        }

        public async Task<bool> SetStatus(string token, ActionTokenStatus status, CancellationToken cancellationToken)
        {
            var repo = _writeRepoFactory.GetLazyRepository<JustGo.MemberProfile.Domain.Entities.ActionToken>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            var setExecutedDateQuery = status == ActionTokenStatus.Executed ? ", ExecuteDate = GETDATE()" : string.Empty;
            var query = $"UPDATE ActionToken SET Status = @Status {setExecutedDateQuery} WHERE Token = @Token";
            await repo.ExecuteAsync(query, cancellationToken, new { Status = status, Token = token }, transaction, "text");

            await _unitOfWork.CommitAsync(transaction);
            return true;
        }


        public async Task<ActionToken> Select(string token, CancellationToken cancellationToken)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            // Use strongly-typed repositories
            var actionTokenRepo = _readRepositoryFactory.GetLazyRepository<ActionToken>().Value;
            var handlerArgRepo = _readRepositoryFactory.GetLazyRepository<ActionTokenHandlerArgument>().Value;
            var tokenRuleRepo = _readRepositoryFactory.GetLazyRepository<TokenRule>().Value;
            var actionTokenLogRepo = _readRepositoryFactory.GetLazyRepository<ActionTokenLog>().Value;

            // Fetch the ActionToken
            var selectQuery = "SELECT * FROM ActionToken WHERE Token = @Token";
            var actionToken = await actionTokenRepo.GetAsync(selectQuery, new { Token = token }, transaction, "text");

            if (actionToken != null)
            {
                // Fetch Handler Arguments
                var handlerArgsQuery = "SELECT * FROM ActionTokenHandlerArguments WHERE ActionTokenId = @Id";
                var handlerArgs = await handlerArgRepo.GetListAsync(handlerArgsQuery, new { Id = actionToken.Id }, transaction, "text");
                actionToken.HandlerArguments = handlerArgs.ToDictionary(arg => arg.Name, arg => arg.Value);

                // Fetch Token Rules
                var tokenRulesQuery = "SELECT * FROM TokenRule WHERE ActionTokenId = @Id";
                var tokenRules = await tokenRuleRepo.GetListAsync(tokenRulesQuery, new { Id = actionToken.Id }, transaction, "text");
                actionToken.TokenRule = tokenRules.ToDictionary(rule => rule.Name, rule => rule.Value);

                // Fetch Action Token Logs
                var actionTokenLogQuery = "SELECT * FROM ActionTokenLog WHERE ActionTokenId = @Id";
                var actionTokenLogs = await actionTokenLogRepo.GetListAsync(actionTokenLogQuery, new { Id = actionToken.Id }, transaction, "text");
                actionToken.Items = actionTokenLogs.ToList();
            }

            return actionToken;
        }

    }
}
