using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V2.Command.SingleNoteUpdate
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
                queryNoteParameters.Add("@ModifiedDate", request.ModifiedDate);
                iSSuccess= await _writeRepository.Value.ExecuteAsync(noteSql, queryNoteParameters, null, "text");
            }
            return iSSuccess>0;
        }
    }
}
