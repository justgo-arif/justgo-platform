using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Queries.GetTransferActivityLog
{

    public class GetTransferActivityLogHandler : IRequestHandler<GetTransferActivityLogQuery, PagedResult<TransferActivityLogItemDTO>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMapper _mapper;
        public GetTransferActivityLogHandler(
            IReadRepositoryFactory readRepository,
            IMapper mapper) 
        {
            _readRepository = readRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<TransferActivityLogItemDTO>> Handle(GetTransferActivityLogQuery request, CancellationToken cancellationToken)
        {

            return await FetchData(request, cancellationToken);

        }


       
        private async Task<PagedResult<TransferActivityLogItemDTO>> FetchData(GetTransferActivityLogQuery request, CancellationToken cancellationToken)
        {

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTransferId", request.AssetTransferId);
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);

            string dataSql =  $@"declare @TransferId int = (select al.AssetOwnershipTransferId 
                                                            from AssetOwnershipTransfers al where 
                                                            al.RecordGuid = @AssetTransferId);
                                select 
                                COUNT(*) OVER() AS TotalRows,
                                Min(se.EventId) ActionId,
                                case 
                                    when se.[Action] = 1 then 'Created'  
                                    when se.[Action] = 2 then 'Updated' 
                                    when se.[Action] = 3 then 'Rejected'
                                    when se.[Action] = 4 then 'Approved'
                                    when se.[Action] = 5 then 'Payment'
                                    when se.[Action] = 6 then 'Change Status'
                                    when se.[Action] = 7 then 'Owner Approved'
                                    else sed.[Name]
                                end  ActionName, 
                                (select [dbo].[GET_UTC_LOCAL_DATE_TIME](DATEADD(SECOND, (DATEDIFF_BIG(SECOND, '19000101', se.AuditDate) / 30) * 30, '19000101'),0)) ActionDate,
                                concat(au.FirstName, ' ', au.LastName) ActionUserFullName,
                                au.MemberId ActionUserMemberId,
                                au.UserSyncId ActionUserId,
                                au.Userid ActionUserDocId,
                                au.ProfilePicURL ActionUserImage,
	                            case 
	                              when 	se.[Action] = 3 and isNull(we.ActionStatus,0) = 2 then we.RejectionReason 
	                              when  se.[Action] = 3 then al.RejectionReason
                                  else ''
	                            end RejectionReason,
                                case 
	                              when 	se.[Action] = 3 and isNull(we.ActionStatus,0) = 2 then we.Remarks 
	                              when  se.[Action] = 3 then  al.RecordRemarks
                                  else ''
	                            end RejectionNote
                                from 
                                SystemEvent se
                                inner join SystemEventData sed on sed.Id = se.Id
                                inner join [user] au on au.Userid = se.ActionUserId
	                            inner join [AssetOwnershipTransfers] al on al.AssetOwnershipTransferId = se.AffectedEntityId
	                            left  join [WorkflowEntities] we on we.WorkflowEntityId = se.OwningEntityId
                                                                     and sed.[Name] = 'Workflow submitted'
                                where 
                                se.category = 29 and
                                se.SubCategory = 4 and 
                                se.AffectedEntityId = @TransferId
							  group by 
							  se.[Action],
							  DATEADD(SECOND, (DATEDIFF_BIG(SECOND, '19000101', se.AuditDate) / 30) * 30, '19000101'),
							  sed.[Name],
							  concat(au.FirstName, ' ', au.LastName),
							  au.MemberId,
							  au.UserSyncId,
							  au.Userid,
							  au.ProfilePicURL,
							  we.ActionStatus,
							  we.RejectionReason,
							  al.RejectionReason,
							  we.Remarks,
							  al.RecordRemarks
                              Order By Min(se.Id) {(request.IsDescending.GetValueOrDefault(false) ? "DESC" : "Asc")}, DATEADD(SECOND, (DATEDIFF_BIG(SECOND, '19000101', se.AuditDate) / 30) * 30, '19000101') {(request.IsDescending.GetValueOrDefault(false) ? "DESC" : "Asc")} 
                              OFFSET ((@PageNumber-1)*@PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY";



            var result = (await _readRepository.GetLazyRepository<TransferActivityLogRawItemDTO>().Value.GetListAsync(dataSql, cancellationToken, queryParameters, null, "text")).ToList();



            return new PagedResult<TransferActivityLogItemDTO>()
            {
                Items = result.Select(r => _mapper.Map<TransferActivityLogItemDTO>(r)).ToList(),
                TotalCount = result.Count > 0 ? result[0].TotalRows : 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

    }
}
