using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetMembersMetadata
{
    public class GetMyListMembersMetadataHandler : IRequestHandler<GetMyListMembersMetadataQuery, PagedResult<MyListMemberDTO>>
    {

        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        public GetMyListMembersMetadataHandler(
            IReadRepositoryFactory readRepository, 
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<PagedResult<MyListMemberDTO>> Handle(GetMyListMembersMetadataQuery request, CancellationToken cancellationToken)
        {

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            string countSql = $@"
                        SELECT 
                        Count(DISTINCT u.UserId) TotalRowCount
                        FROM [User] u
						left join Family_Links fm on fm.Entityid = u.MemberDocId
                        Where 
                        (u.UserId = {currentUserId}
                         or
                         fm.Docid in (Select DocId  
                              from  Family_Links fl where fl.Entityid = (
                                Select MemberDocId from [User]
                                where userid = {currentUserId})))
                        and
                        (Concat(u.FirstName, ' ', u.LastName) like @query or
                        u.MemberId like @query or
                        u.EmailAddress like @query)
                        ";

            string sql = $@"
                        SELECT DISTINCT 
                        u.UserId Id,
                        u.MemberDocId MemberDocId,
                        u.MemberId MID,
                        u.FirstName FirstName,
                        u.LastName LastName,
                        cast(u.UserSyncId as nvarchar(255)) UserId,
                        u.EmailAddress EmailAddress,
                        u.Gender Gender,
                        u.ProfilePicURL [Image],
                        case when u.UserId = {currentUserId} then 1 else 0 end IsLoggedInUser
                        FROM [User] u 
						left join Family_Links fm on fm.Entityid = u.MemberDocId
                        Where 
                        (u.UserId = {currentUserId}
                         or
                         fm.Docid in (Select DocId  
                              from  Family_Links fl where fl.Entityid = (
                                Select MemberDocId from [User]
                                where userid = {currentUserId})))
                        and
                        (Concat(u.FirstName, ' ', u.LastName) like @query or
                        u.MemberId like @query or
                        u.EmailAddress like @query)
                        Order By u.FirstName, u.LastName
                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                        FETCH NEXT @PageSize ROWS ONLY";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);
            queryParameters.Add("@Query", $@"%{request.Query}%");
            var rows =(await _readRepository.GetLazyRepository<MyListMemberDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            var rowsCount = (int)(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(countSql, cancellationToken, queryParameters, null, "text"));


            return new PagedResult<MyListMemberDTO>(){
                TotalCount = rowsCount,
                Items = rows
            };
        }
    }
}
