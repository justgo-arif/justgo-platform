using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Class.V3.Command.MemberNoteDelete;
using MobileApps.Domain.Entities.V2;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Command.SingleNoteDelete  
{
    class MemberNoteDeleteCommandHandler : IRequestHandler<MemberNoteDeleteCommand, bool>
    {
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        public MemberNoteDeleteCommandHandler(LazyService<IWriteRepository<object>> writeRepository)
        {
            _writeRepository = writeRepository;
        }

        public async Task<bool> Handle(MemberNoteDeleteCommand request, CancellationToken cancellationToken)
        {
            string sql = $@"Update [MemberNotes] set IsActive=0 where NotesId=@MemberNoteId;";
            var queryNoteParameters = new DynamicParameters();
            queryNoteParameters.Add("@MemberNoteId", request.MemberNoteId);
            var result = await _writeRepository.Value.ExecuteAsync(sql, queryNoteParameters, null, "text");

            return result > 0;
        }
    }
       
}
