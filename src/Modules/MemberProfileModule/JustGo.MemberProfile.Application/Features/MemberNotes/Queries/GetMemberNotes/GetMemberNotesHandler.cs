using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetNotesCategory;
using JustGo.MemberProfile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetMemberNotes
{
    public class GetMemberNotesHandler : IRequestHandler<GetMemberNotesQuery, GetMemberNotesDto>
    {
        private readonly LazyService<IReadRepository<GetMemberNotesDataDto>> _readRepository;
        private readonly IUtilityService _utilityService;

        public GetMemberNotesHandler(
            LazyService<IReadRepository<GetMemberNotesDataDto>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<GetMemberNotesDto> Handle(GetMemberNotesQuery request, CancellationToken cancellationToken = default)
        {
            int ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid, cancellationToken);
            string sql = """
                         DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
                         DECLARE @Timezone int = (SELECT [Value] FROM SystemSettings Where ItemKey = 'ORGANISATION.TIMEZONE')
                                                  
                         SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);
                         SELECT
                             mn.NotesId                          AS MemberNoteId,
                             mn.NotesGuid AS MemberNoteGuid,
                             mn.EntityId                              AS MemberId,
                             COALESCE(createdByUser.firstname + ' '+ createdByUser.lastname, null)              AS MemberName,
                             IIF(ISNULL(createdByUser.ProfilePicURL, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', createdByUser.ProfilePicURL, '&t=user&p=', createdByUser.UserId)) AS ProfilePicURL,
                             mn.NoteCategoryId                        AS CategoryId,
                             nc.NoteCategoryName                      AS CategoryName,
                             mn.MemberNoteTitle                       AS NoteTitle,
                             mn.Details                               AS Details,
                         
                             DATEADD(SECOND, tz.gm_offset, mn.CreatedDate) AS CreatedDate,
                             mn.IsActive,
                             mn.IsHide
                         FROM MemberNotes mn
                         inner JOIN [User] u ON mn.EntityId = u.UserId
                         INNER JOIN [User] createdByUser ON mn.UserId = createdByUser.UserId
                         inner JOIN NoteCategories nc ON mn.NoteCategoryId = nc.NoteCategoryId
                         
                         OUTER APPLY (
                             SELECT TOP 1 gm_offset, abbreviation
                             FROM Timezone
                             WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01 00:00:00', mn.CreatedDate) AS BIGINT) * 3600
                               AND zone_id = @Timezone
                             ORDER BY time_start DESC
                         ) AS tz
                         WHERE u.UserSyncId = @UserSyncId
                           AND mn.OwnerId = @OwnerId
                           AND (@CategoryId = 0 OR mn.NoteCategoryId = @CategoryId)
                           AND ISNULL(mn.IsActive, 1) = 1
                         ORDER BY mn.CreatedDate DESC;
                         """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("UserSyncId", request.UserSyncId);
            queryParameters.Add("OwnerId", ownerId);
            queryParameters.Add("CategoryId", request.CategoryId);

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken,queryParameters, commandType: "text")).ToList();

            return new GetMemberNotesDto
            {
                MemberNotes = result,
                TotalCounts = result.Count
            };
        }
    }
}
