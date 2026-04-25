using Dapper;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Commands.ChangeStatusOfNotes
{
    public class ChangeStatusOfNotesHandler : IRequestHandler<ChangeStatusOfNotesCommand, Result<string>>
    {
        private readonly IWriteRepositoryFactory _writeRepoFactory;
        private readonly IUtilityService _utilityService;
        private const string CHANGE_STATUS = """
                                             IF @IsActive IS NOT NULL
                                             BEGIN
                                                 UPDATE [dbo].[MemberNotes]
                                                 SET IsActive = @IsActive
                                                 WHERE NotesGuid = @MemberNoteGuid;
                                             END

                                             IF @IsHide IS NOT NULL
                                             BEGIN
                                                 UPDATE [dbo].[MemberNotes]
                                                 SET IsHide = @IsHide
                                                 WHERE NotesGuid = @MemberNoteGuid;
                                             END
                                             """;

        public ChangeStatusOfNotesHandler(IWriteRepositoryFactory writeRepoFactory,
            IUtilityService utilityService)
        {
            _writeRepoFactory = writeRepoFactory;
            _utilityService = utilityService;
        }

        public async Task<Result<string>> Handle(ChangeStatusOfNotesCommand request,
            CancellationToken cancellationToken = default)
        {

            try
            {
                var dataRepo = _writeRepoFactory.GetRepository<dynamic>();
                await _utilityService.GetCurrentUserId(cancellationToken);
                var parameters = new DynamicParameters();
                parameters.Add("@MemberNoteGuid", request.MemberNoteGuid);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@IsHide", request.IsHide);

                var result = await dataRepo.ExecuteScalarAsync<int>(CHANGE_STATUS, cancellationToken, parameters, null, "Text");

                var action = !request.IsActive.HasValue ? "Deleted" : request.IsHide == true ? "Hide" : "Shown";
                return $"Member note {action} successfully";

            }
            catch (Exception e)
            {
                return Result<string>.Failure($"An error occurred while changing status of member note: {e.Message}", ErrorType.InternalServerError);
            }
        }
    }
}
