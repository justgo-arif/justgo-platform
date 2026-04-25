using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Class.V3.Command.MemberNoteUpsert;

namespace MobileApps.Application.Features.Class.V3.Command.MemberNoteUpsert
{
    class SingleNoteUpdateCommandHandler : IRequestHandler<MemberNoteUpsertCommand, bool>
    {
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
  
        public SingleNoteUpdateCommandHandler(LazyService<IWriteRepository<object>> writeRepository)
        {
            _writeRepository = writeRepository;
        }

        public async Task<bool> Handle(MemberNoteUpsertCommand request, CancellationToken cancellationToken)
        {
            int iSSuccess = 0;
            string noteSql = @"MERGE INTO MemberNotes AS target
            USING (
                SELECT
                    @MemberNoteId AS MemberNoteId
            ) AS source ON (target.NotesId = source.NotesId AND source.NotesId IS NOT NULL)
            WHEN MATCHED THEN 
                UPDATE SET
                    NotesGuid = @MemberNoteGuid,
                    EntityType = @EntityType,
                    EntityId = @EntityId,
                    Details = @Details,
                    UserId = @UserId,
                    CreatedDate = @CreatedDate,
                    MemberNoteTitle = @MemberNoteTitle,
                    OwnerId = @OwnerId,
                    NoteCategoryId = @NoteCategoryId,
                    IsActive = @IsActive,
                    IsHide = @IsHide
            WHEN NOT MATCHED THEN
                INSERT (
                    NotesGuid,
                    EntityType,
                    EntityId,
                    Details,
                    UserId,
                    CreatedDate,
                    MemberNoteTitle,
                    OwnerId,
                    NoteCategoryId,
                    IsActive,
                    IsHide
                )
                VALUES (
                    @MemberNoteGuid,
                    @EntityType,
                    @EntityId,
                    @Details,
                    @UserId,
                    @CreatedDate,
                    @MemberNoteTitle,
                    @OwnerId,
                    @NoteCategoryId,
                    @IsActive,
                    @IsHide
                );";

            
                // For UpdateNoteSql
                var queryNoteParameters = new DynamicParameters();
                queryNoteParameters.Add("@MemberNoteId", request.MemberNoteId);
                queryNoteParameters.Add("@MemberNoteGuid", request.MemberNoteGuid ?? Guid.NewGuid());
                queryNoteParameters.Add("@EntityType", "User");
                queryNoteParameters.Add("@EntityId", request.UserId);
                queryNoteParameters.Add("@Details", request.Details);
                queryNoteParameters.Add("@UserId", request.CreatedBy);
                queryNoteParameters.Add("@CreatedDate", DateTime.UtcNow);
                queryNoteParameters.Add("@MemberNoteTitle", request.MemberNoteTitle);
                queryNoteParameters.Add("@OwnerId", request.OwnerId);
                queryNoteParameters.Add("@NoteCategoryId", request.NoteCategoryId);
                queryNoteParameters.Add("@IsActive", request.IsActive);
                queryNoteParameters.Add("@IsHide", request.IsHide);
               
            iSSuccess = await _writeRepository.Value.ExecuteAsync(noteSql, queryNoteParameters, null, "text");
            return iSSuccess>0;
        }
    }
}
