using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Domain.Entities.V2.Classes;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetAttendeeNoteList
{
    class MemberNoteListQueryHandler : IRequestHandler<MemberNoteListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public MemberNoteListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(MemberNoteListQuery request, CancellationToken cancellationToken)
        {
            string sql = $@"SELECT 
            mn.NotesId as MemberNoteId,
            mn.EntityId as UserId,
            nc.NoteCategoryId,
            nc.NoteCategoryName,
            mn.MemberNoteTitle,
            mn.Details,
            FORMAT(CAST(dbo.[GET_UTC_LOCAL_DATE_TIME](mn.CreatedDate, null) as DateTime), 'MMM dd, yyyy ''at'' hh:mm tt') AS CreatedDate,
            mn.NotesGuid as MemberNoteGuid,
            mn.EntityType,
            crd.UserId as CreatedBy,
            CONCAT(crd.FirstName, ' ', crd.LastName) AS CreatedByName,
            mn.OwnerId,
            mn.IsActive,
            mn.IsHide,
            crd.Gender,
            crd.ProfilePicURL
        FROM MemberNotes mn
        INNER JOIN NoteCategories nc ON mn.NoteCategoryId = nc.NoteCategoryId
        INNER JOIN [User] u ON mn.EntityId = u.Userid
        inner join [User] crd ON mn.UserId = crd.Userid
        WHERE u.UserSyncId =  @UserGuid AND 
        nc.IsActive = 1
            AND mn.IsActive = 1
              
        ORDER BY mn.CreatedDate DESC;";        

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserGuid", request.UserGuid);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result))??new List<IDictionary<string, object>>();

        }
        

    }
}
