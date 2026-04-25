using Dapper;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;


namespace JustGo.MemberProfile.Application.Features.MemberNotes.Commands.SaveMemberNotes
{
    
    public class SaveMemberNotesHandler : IRequestHandler<SaveMemberNotesCommand, Result<string>>
    {
        private readonly IWriteRepositoryFactory _writeRepoFactory;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IMediator _mediator;
        private const string INSERT_OR_UPDATE = """
                                                IF @MemberNoteId = 0
                                                BEGIN
                                                    INSERT INTO [dbo].[MemberNotes] (
                                                        [NotesGuid],
                                                        [EntityType],
                                                        [EntityId],
                                                        [Details],
                                                        [UserId],
                                                        [CreatedDate],
                                                        [MemberNoteTitle],
                                                        [OwnerId],
                                                        [NoteCategoryId],
                                                        [IsActive],
                                                        [IsHide]
                                                    )
                                                    VALUES (
                                                        NEWID(),
                                                        @EntityType,
                                                        @EntityId,
                                                        @Details,
                                                        @UserId,
                                                        GETUTCDATE(),
                                                        @MemberNoteTitle,
                                                        @OwnerId,
                                                        @NoteCategoryId,
                                                        1,
                                                        0
                                                    )
                                                END
                                                ELSE
                                                BEGIN
                                                    UPDATE [dbo].[MemberNotes]
                                                    SET
                                                        [EntityId] = @EntityId,
                                                        [Details] = @Details,
                                                        [UserId] = @UserId,
                                                        [MemberNoteTitle] = @MemberNoteTitle,
                                                        [OwnerId] = @OwnerId,
                                                        [NoteCategoryId] = @NoteCategoryId
                                                    WHERE [NotesId] = @MemberNoteId
                                                END
                                                """;

        public SaveMemberNotesHandler(IWriteRepositoryFactory writeRepoFactory,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService, IMediator mediator)
        {
            _writeRepoFactory = writeRepoFactory;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _mediator = mediator;
        }

        public async Task<Result<string>> Handle(SaveMemberNotesCommand request,
            CancellationToken cancellationToken = default)
        {

            try
            {
                var dataRepo = _writeRepoFactory.GetRepository<Domain.Entities.MemberNotes>();
                var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
                var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid, cancellationToken);
                var entityId = await _utilityService.GetUserIdByUserSyncGuidAsync(request.EntityId, cancellationToken);
                var parameters = new DynamicParameters();
                parameters.Add("@MemberNoteId", request.MemberNoteId, DbType.Int32);
                parameters.Add("@EntityType", 8);
                parameters.Add("@EntityId", entityId);
                parameters.Add("@Details", request.Details);
                parameters.Add("@UserId", currentUserId, DbType.Int32);
                parameters.Add("@MemberNoteTitle", request.NoteTitle);
                parameters.Add("@OwnerId", ownerId, DbType.Int32);
                parameters.Add("@NoteCategoryId", request.CategoryId, DbType.Int32);

                var result = await dataRepo.ExecuteScalarAsync<int>(INSERT_OR_UPDATE, cancellationToken, parameters, null, "Text");

                var action = request.MemberNoteId == 0  ? "created" : "updated";
                return $"Member note {action} successfully";

            }
            catch (Exception e)
            {
                return Result<string>.Failure($"An error occurred while saving member note: {e.Message}", ErrorType.InternalServerError);
            }
        }
    }
}
