using Dapper;
using JustGo.Authentication.Infrastructure.Notes;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MapsterMapper;

namespace AuthModule.Application.Features.Notes.Commands.EditNotes
{
    public class EditNotesHandler : IRequestHandler<EditNotesCommand, int>
    {
        private readonly INoteService _noteService;
        private readonly IMapper _mapper;
        private readonly IWriteRepositoryFactory _writeRepository;
        public EditNotesHandler(INoteService noteService, IMapper mapper, IWriteRepositoryFactory writeRepository)
        {
            _noteService = noteService;
            _mapper = mapper;
            _writeRepository = writeRepository;
        }

        public async Task<int> Handle(EditNotesCommand request, CancellationToken cancellationToken)
        {
            var note = _mapper.Map<Note>(request);
            int noOfRowsAffected = await _noteService.EditNote(note, cancellationToken);

            if (noOfRowsAffected > 0 && request.IsMailSend == true && request.Module == "Member") // Module "Member" is the one for which we want to send an email when a note is updated.
            {
                await DropMailToQueue(request, cancellationToken);
            }
            return noOfRowsAffected;
        }

        private async Task DropMailToQueue(EditNotesCommand request, CancellationToken cancellationToken)
        {
            string sql = @"
            DECLARE 
            @Subject varchar(1000),
            @To varchar(100);
                
            SET @Subject = 'A note is updated to your profile';

            SELECT @To = EmailAddress 
            FROM [User] U 
            WHERE U.UserSyncId = @UserSyncId;

            IF(ISNULL(@To, '') != '')
            BEGIN
                INSERT INTO MailQueue (Sender, [To], [Subject], Tag, Mailbody, [Status], CreatedDate, FailCount)
                Values ('noreply@justgo.com', @To, @Subject, 'Member Profile Note', @Mailbody, 1, getdate(), 0)
            END
            ";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("Mailbody", request.Details);
            queryParameters.Add("UserSyncId", request.EntityId);
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
