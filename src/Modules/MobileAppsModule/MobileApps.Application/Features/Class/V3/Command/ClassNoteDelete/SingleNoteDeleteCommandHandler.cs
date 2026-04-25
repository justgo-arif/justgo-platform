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

namespace MobileApps.Application.Features.Class.V3.Command.ClassNoteDelete
{
    class SingleNoteDeleteCommandHandler : IRequestHandler<SingleNoteDeleteCommand, bool>
    {
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public SingleNoteDeleteCommandHandler(LazyService<IWriteRepository<object>> writeRepository, LazyService<IReadRepository<object>> readRepository)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(SingleNoteDeleteCommand request, CancellationToken cancellationToken)
        {
            int iSSuccess = 0;
            string noteSql = @"DELETE FROM JustGoBookingAttendeeDetailNote WHERE AttendeeDetailNoteId = @AttendeeDetailNoteId;";

            if (request.NoteId > 0)
            {
                // For DeleteNoteSql
                var queryNoteParameters = new DynamicParameters();
                queryNoteParameters.Add("@AttendeeDetailNoteId", request.NoteId);
                iSSuccess= await _writeRepository.Value.ExecuteAsync(noteSql, queryNoteParameters, null, "text");
            }
            return iSSuccess>0;
        }
    }
}
