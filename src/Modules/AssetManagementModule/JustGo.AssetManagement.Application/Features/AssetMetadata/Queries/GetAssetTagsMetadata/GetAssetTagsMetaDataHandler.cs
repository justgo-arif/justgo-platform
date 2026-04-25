using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetTagsMetadata
{
    public class GetAssetTagsMetaDataHandler : IRequestHandler<GetAssetTagsMetaDataQuery, PagedResult<SelectListItemDTO<string>>>
    {

        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMediator _mediator;

        public GetAssetTagsMetaDataHandler(IReadRepositoryFactory readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PagedResult<SelectListItemDTO<string>>> Handle(GetAssetTagsMetaDataQuery request, CancellationToken cancellationToken)
        {

            int AssetTypeId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetTypes, RecordGuids = new List<string>() { request.AssetTypeId } }))[0];

            string countSql = @"Select Count(*) TotalRowCount from 
                            AssetTypesTag att
                            Where att.[Name] like @Query and att.AssetTypeId = @AssetTypeId";

            string sql = @"Select * from 
                            AssetTypesTag att
                            Where att.[Name] like @Query and att.AssetTypeId = @AssetTypeId
                            Order By att.[Name]
                            OFFSET (@PageNumber - 1) * @PageSize ROWS
                            FETCH NEXT @PageSize ROWS ONLY";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);
            queryParameters.Add("@AssetTypeId", AssetTypeId);
            queryParameters.Add("@Query", $@"%{request.Query}%");
            var tags =(await _readRepository.GetLazyRepository<AssetTypesTag>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            var tagsCount = (int)(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(countSql, cancellationToken, queryParameters, null, "text"));


            return new PagedResult<SelectListItemDTO<string>>(){
                TotalCount = tagsCount,
                Items = tags.Select(r =>
                   new SelectListItemDTO<string>()
                   {
                       Text = r.Name,
                       Value = r.RecordGuid,
                   }
                ).ToList()
            };
        }
    }
}
