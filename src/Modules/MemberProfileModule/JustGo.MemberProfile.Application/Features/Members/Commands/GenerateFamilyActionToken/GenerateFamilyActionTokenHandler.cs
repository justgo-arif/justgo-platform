using Dapper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.GenerateFamilyActionToken
{
    public class GenerateFamilyActionTokenHandler : IRequestHandler<GenerateFamilyActionTokenCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepoFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GenerateFamilyActionTokenHandler(
            IWriteRepositoryFactory writeRepoFactory,
            IUnitOfWork unitOfWork,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService,
            IHttpContextAccessor httpContextAccessor)
        {
            _writeRepoFactory = writeRepoFactory;
            _unitOfWork = unitOfWork;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> Handle(GenerateFamilyActionTokenCommand request, CancellationToken cancellationToken)
        {
            var repo = _writeRepoFactory.GetLazyRepository<object>().Value;
            int id = -1;
            string redirectUrl = $"{request.Url}api/v1/members/family-link-feedback?message=Thanks, you have been successfully linked to your family";
            string approverUrl = $"{request.Url}api/v1/members/family-action-token/invoke?token=";

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Insert ActionToken
                var insertTokenSql = "INSERT INTO ActionToken(HandlerType, CreateDate, [Status], VaildFor) OUTPUT INSERTED.Id values(@HandlerType, GETDATE(), 0, 20000)";
                var tokenParams = new DynamicParameters();
                tokenParams.Add("@HandlerType", "FamilyManager");

                var readRepo = _readRepository.GetLazyRepository<object>().Value;
                var result = await readRepo.GetSingleAsync(insertTokenSql, cancellationToken, tokenParams, transaction, "text");
                id = Convert.ToInt32(result);


                // Generate the token
                string token = CalculateMD5Hash($"{request.FamilyDocId}FamilyManager{id}");

                // Update ActionToken and insert related arguments
                var updateTokenSql = @"UPDATE ActionToken SET Token = @Token WHERE Id = @Id
                                       INSERT INTO ActionTokenHandlerArguments(ActionTokenId, Name, Value) 
                                       SELECT @Id, 'FamilyDocId', @FamilyDocId
                                       INSERT INTO ActionTokenHandlerArguments(ActionTokenId, Name, Value) 
                                       SELECT @Id, 'InitiateMemberDocId', @InitiateMemberDocId
                                       INSERT INTO ActionTokenHandlerArguments(ActionTokenId, Name, Value) 
                                       SELECT @Id, 'TargetMemberDocId', @TargetMemberDocId
                                       INSERT INTO ActionTokenHandlerArguments(ActionTokenId, Name, Value) 
                                       SELECT @Id, 'RedirectUrl', @RedirectUrl";

                var updateParams = new DynamicParameters();
                updateParams.Add("@Id", id);
                updateParams.Add("@Token", token);
                updateParams.Add("@FamilyDocId", request.FamilyDocId);
                updateParams.Add("@InitiateMemberDocId", request.InitiateMemberDocId);
                updateParams.Add("@TargetMemberDocId", request.TargetMemberDocId);
                updateParams.Add("@RedirectUrl", redirectUrl);

                await repo.ExecuteAsync(updateTokenSql, updateParams, transaction, "text");

                // Get tenantClientId from claims or HttpContext.Items
                var context = _httpContextAccessor.HttpContext;
                var tenantClientId = context?.User?.Claims?.FirstOrDefault(c => c.Type == "TenantClientId")?.Value
                    ?? context?.Items["tenantClientId"] as string;

                string encryptedTenantId = string.Empty;
                if (!string.IsNullOrEmpty(tenantClientId))
                {
                    encryptedTenantId = _utilityService.EncryptData(tenantClientId);
                }

                var urlWithTenant = string.IsNullOrEmpty(encryptedTenantId)
                    ? $"{approverUrl}{token}"
                    : $"{approverUrl}{token}&tenantClientId={encryptedTenantId}";

                // Send the notification email
                var emailParams = new DynamicParameters();
                emailParams.Add("SourceDocId", request.InitiateMemberDocId);
                emailParams.Add("TargetDocId", request.TargetMemberDocId);
                emailParams.Add("URL", urlWithTenant);

                await repo.ExecuteAsync("[SendFamilyLinkNotificationEmail]", emailParams, transaction);

                // Fetch UserIds for SourceDocId and TargetDocId
                //var sqlForSourceUserId = @"SELECT u.UserId FROM [user] u
                //            INNER JOIN EntityLink el ON el.SourceId = u.Userid AND el.LinkId = @SourceDocId";
                //var sqlForTargetUserId = @"SELECT UserId FROM [user] u
                //            INNER JOIN EntityLink el ON el.SourceId = u.Userid AND el.LinkId = @TargetDocId";

                var sqlForSourceUserId = @"SELECT u.UserId 
                          FROM [user] u
                          INNER JOIN EntityLink el ON el.SourceId = u.Userid 
                          AND el.LinkId = @SourceDocId";

                var sqlForTargetUserId = @"SELECT u.UserId 
                          FROM [user] u
                          INNER JOIN EntityLink el ON el.SourceId = u.Userid 
                          AND el.LinkId = @TargetDocId";

                var sourceUserIdParams = new DynamicParameters();
                sourceUserIdParams.Add("@SourceDocId", request.InitiateMemberDocId, dbType: DbType.Int32);

                var sourceUserObj = await _readRepository.GetLazyRepository<dynamic>().Value.GetAsync(
                    sqlForSourceUserId, cancellationToken, sourceUserIdParams, null, "text");
                int? sourceUserId = sourceUserObj != null ? (int)sourceUserObj.UserId : (int?)null;

                var targetUserIdParams = new DynamicParameters();
                targetUserIdParams.Add("@TargetDocId", request.TargetMemberDocId, dbType: DbType.Int32);

                var targetUserObj = await _readRepository.GetLazyRepository<dynamic>().Value.GetAsync(
                    sqlForTargetUserId, cancellationToken, targetUserIdParams, null, "text");
                int? targetUserId = targetUserObj != null ? (int)targetUserObj.UserId : (int?)null;

                // Send final email using parameters
                var finalEmailParams = new DynamicParameters();
                finalEmailParams.Add("@ForEntityId", request.TargetMemberDocId);
                finalEmailParams.Add("@InvokeUserId", sourceUserId);
                finalEmailParams.Add("@MessageScheme", "Family\\Link Request");
                finalEmailParams.Add("@GetInfo", 0);
                finalEmailParams.Add("@OwnerType", "NGB");
                finalEmailParams.Add("@OwnerId", 0);
                finalEmailParams.Add("@Argument", urlWithTenant);

                await repo.ExecuteAsync("SEND_EMAIL_BY_SCHEME", finalEmailParams, transaction);

                await _unitOfWork.CommitAsync(transaction);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync(transaction);
                throw;
            }

            return id;
        }

        private static string CalculateMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}