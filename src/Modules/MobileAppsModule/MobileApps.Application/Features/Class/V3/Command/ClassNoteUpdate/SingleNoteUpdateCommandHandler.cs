using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace MobileApps.Application.Features.Class.V3.Command.ClassNoteUpdate
{
    class SingleNoteUpdateCommandHandler : IRequestHandler<SingleNoteUpdateCommand, bool>
    {
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public SingleNoteUpdateCommandHandler(LazyService<IWriteRepository<object>> writeRepository, LazyService<IReadRepository<object>> readRepository)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(SingleNoteUpdateCommand request, CancellationToken cancellationToken)
        {
            int iSSuccess = 0;
            string noteSql = @"UPDATE JustGoBookingAttendeeDetailNote
             SET Note = @Note,ModifiedDate = @ModifiedDate
             WHERE AttendeeDetailNoteId = @AttendeeDetailNoteId;";

            if (request.AttendeeDetailNoteId > 0)
            {
                // For UpdateNoteSql
                var queryNoteParameters = new DynamicParameters();
                queryNoteParameters.Add("@AttendeeDetailNoteId", request.AttendeeDetailNoteId);
                queryNoteParameters.Add("@Note", request.Note);
                queryNoteParameters.Add("@ModifiedDate", DateTime.UtcNow);
                iSSuccess= await _writeRepository.Value.ExecuteAsync(noteSql, queryNoteParameters, null, "text");
            }
            return iSSuccess>0;
        }
    }
}
