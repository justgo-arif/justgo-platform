using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using Dapper;
using FluentValidation;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberEmergencyContactBySyncGuid
{
    // Validates the query by invoking the authorization stored procedure [IsActionAllowed].
    // If the procedure denies the action, validation fails with the returned reason.
    public class GetMemberEmergencyContactBySyncGuidQueryValidator : AbstractValidator<GetMemberEmergencyContactBySyncGuidQuery>
    {
        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetMemberEmergencyContactBySyncGuidQueryValidator(
            IMediator mediator,
            IWriteRepositoryFactory writeRepositoryFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _writeRepositoryFactory = writeRepositoryFactory;
            _httpContextAccessor = httpContextAccessor;

            // Basic guard: SyncGuid must be provided
            RuleFor(q => q.Id)
                .NotEqual(Guid.Empty)
                .WithMessage("Member id is required.");

            // Custom async rule: call IsActionAllowed
            RuleFor(q => q)
                .CustomAsync(async (q, context, cancellationToken) =>
                {
                    // Resolve InvokingUserId (current user) and MemberDocId (DocId) from the query Guid.
                    // Replace these two lines with your actual queries/services that provide the required ids.
                    var invokingUserId = await ResolveInvokingUserIdAsync(cancellationToken);
                    var memberDocId = await ResolveMemberDocIdFromSyncGuidAsync(q.Id, cancellationToken);

                    if (invokingUserId <= 0)
                    {
                        context.AddFailure("Authorization", "Invoking user could not be resolved.");
                        return;
                    }

                    if (memberDocId <= 0)
                    {
                        context.AddFailure("Authorization", "Target member document could not be resolved.");
                        return;
                    }

                    var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
                    var p = new DynamicParameters();
                    p.Add("@InvokingUserId", invokingUserId, DbType.Int32);
                    p.Add("@DocId", memberDocId, DbType.Int32);
                    p.Add("@Result", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                    await repo.ExecuteAsync(
                        qry: "dbo.IsActionAllowed @InvokingUserId,@DocId,'',@Result OUTPUT",
                        cancellationToken: cancellationToken,
                        dynamicParameters: p,
                        dbTransaction: null,
                        commandType: "StoredProcedure");

                    var resultText = p.Get<string>("@Result");

                    if (!string.IsNullOrWhiteSpace(resultText))
                    {
                        context.AddFailure("Authorization", resultText);
                    }
                });
        }

        private async Task<int> ResolveInvokingUserIdAsync(CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var user = httpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("No authenticated user.");
            var userSyncId = user.FindFirst("UserSyncId")?.Value;
            if (string.IsNullOrWhiteSpace(userSyncId))
                throw new UnauthorizedAccessException("User Sync ID not found.");

            var entity = await _mediator.Send(new GetUserByUserSyncIdQuery(new Guid(userSyncId)), cancellationToken);
            return entity.Userid;
        }
        private async Task<int> ResolveMemberDocIdFromSyncGuidAsync(Guid syncGuid, CancellationToken cancellationToken)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;

            const string sql = 
                """
                SELECT ISNULL(MAX(MemberDocId), 0) FROM [dbo].[User] WHERE UserSyncId = @SyncGuid;
                """;
            var p = new DynamicParameters();
            p.Add("@SyncGuid", syncGuid, DbType.Guid);

            var docId = await repo.ExecuteQuerySingleAsync<int>(sql,cancellationToken,p,dbTransaction: null,commandType: "text");

            return docId;
        }
 
    }
}