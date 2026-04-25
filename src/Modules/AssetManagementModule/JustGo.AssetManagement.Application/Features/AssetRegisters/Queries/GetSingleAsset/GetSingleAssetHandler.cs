using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetGuidMapById;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using System.Data;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset
{

    public partial class GetSingleAssetHandler : IRequestHandler<GetSingleAssetQuery, AssetDTO>
    {

        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly IUtilityService _utilityService;
        public GetSingleAssetHandler(
            IReadRepositoryFactory readRepository,
            IMapper mapper,
            IMediator mediator,
            IUtilityService utilityService)
        {

            _readRepository = readRepository;
            _mapper = mapper;
            _mediator = mediator;
            _utilityService = utilityService;
        }

        public async Task<AssetDTO> Handle(GetSingleAssetQuery request, CancellationToken cancellationToken)
        {

            var rawData = await FetchRawData(request, cancellationToken);

            // Start all async tasks in parallel
            var resultTask = GetResultFromRawData(rawData, cancellationToken);
            var credentialsTask = GetAssetCredentials(request.AssetRegisterId, cancellationToken);
            var ownersTask = GetAssetOwners(request.AssetRegisterId, cancellationToken);

            // Wait for all to complete
            await Task.WhenAll(resultTask, credentialsTask, ownersTask);

            // Now retrieve the completed results
            var result = await resultTask;
            result.AssetCredentials = await credentialsTask;
            result.AssetOwners = await ownersTask;

            return result;

        }

        private async Task<AssetDTO> GetResultFromRawData(AssetDTOWithRawData rawData, CancellationToken cancellationToken)
        {

            if(rawData == null)
            {
                return null;
            }

            var result = _mapper.Map<AssetDTO>(rawData);
            result.AssetImages =
            rawData.Images != null ?
                rawData.Images.Split("||").Select(r =>
                JsonConvert.DeserializeObject<AssetImageDTO>(r,
                    new JsonSerializerSettings
                    {
                        Error = (s, e) => e.ErrorContext.Handled = true
                    })
                ).Where(r => r is not null).ToList() : [];

            result.AssetTags =
            rawData.Tags != null ?
            rawData.Tags.Split(',').Select(r => r).ToList() : [];

           // result.AssetCredentials = await GetAssetCredentials(result.AssetRegisterId, cancellationToken);
           // result.AssetOwners = await GetAssetOwners(result.AssetRegisterId, cancellationToken);


            if (!String.IsNullOrEmpty(rawData.PrimaryLicenseInfo))
            {
                result.PrimaryLicenses = rawData.PrimaryLicenseInfo.Split("||").Select(r =>
                        JsonConvert.DeserializeObject<AssetLicenseDTO>(r,
                    new JsonSerializerSettings
                    {
                        Error = (s, e) => e.ErrorContext.Handled = true
                    })
                ).Where(r => r is not null).ToList();



            }

            if (!String.IsNullOrEmpty(rawData.AdditionalLicenseInfo))
            {
                result.AdditionalLicenses = rawData.AdditionalLicenseInfo.Split("||").Select(r =>
                        JsonConvert.DeserializeObject<AssetLicenseDTO>(r,
                    new JsonSerializerSettings
                    {
                        Error = (s, e) => e.ErrorContext.Handled = true
                    })
                ).Where(r => r is not null).ToList();

            }

            var ids = result.PrimaryLicenses!=null ? result.PrimaryLicenses.Select(r => decimal.Parse(r.ProductId)).ToList()
                      .Concat(result.PrimaryLicenses.Select(r => decimal.Parse(r.LicenseId)).ToList())
                      .Concat(result.PrimaryLicenses.Select(r => decimal.Parse(r.OwnerId ?? "0")).ToList()).ToList():null;
            if(result.AdditionalLicenses!=null)
                ids= ids==null? result.AdditionalLicenses.Select(r => decimal.Parse(r.ProductId)).ToList()
                   .Concat(result.AdditionalLicenses.Select(r => decimal.Parse(r.LicenseId)).ToList())
                   .Concat(result.AdditionalLicenses.Select(r => decimal.Parse(r.OwnerId ?? "0")).ToList()).ToList():
                         ids.Concat(result.AdditionalLicenses.Select(r => decimal.Parse(r.ProductId)).ToList())
                   .Concat(result.AdditionalLicenses.Select(r => decimal.Parse(r.LicenseId)).ToList())
                   .Concat(result.AdditionalLicenses.Select(r => decimal.Parse(r.OwnerId ?? "0")).ToList()).ToList();

            if(ids != null)
            if (ids.Any())
            {

                var guidMaps = await _mediator.Send(new GetGuidMapByIdQuery()
                {
                    Entity = AssetTables.Document,
                    Ids = ids,
                });

                var guidMapsDict = guidMaps.ToDictionary(r => r.Key, r => r.Value);

             if(result.PrimaryLicenses != null)
               foreach (var item in result.PrimaryLicenses)
                {

                    item.LicenseId = guidMapsDict.ContainsKey(decimal.Parse(item.LicenseId)) ?
                                     guidMapsDict[decimal.Parse(item.LicenseId)] : null;

                    item.OwnerId = guidMapsDict.ContainsKey(decimal.Parse(item.OwnerId ?? "0")) ?
                     guidMapsDict[decimal.Parse(item.OwnerId)] : null;

                    item.ProductId = guidMapsDict.ContainsKey(decimal.Parse(item.ProductId)) ?
                     guidMapsDict[decimal.Parse(item.ProductId)] : null;

                }

                if(result.AdditionalLicenses != null)
                foreach (var item in result.AdditionalLicenses)
                {

                    item.LicenseId = guidMapsDict.ContainsKey(decimal.Parse(item.LicenseId)) ?
                                     guidMapsDict[decimal.Parse(item.LicenseId)] : null;

                    item.OwnerId = guidMapsDict.ContainsKey(decimal.Parse(item.OwnerId ?? "0")) ?
                     guidMapsDict[decimal.Parse(item.OwnerId)] : null;

                    item.ProductId = guidMapsDict.ContainsKey(decimal.Parse(item.ProductId)) ?
                     guidMapsDict[decimal.Parse(item.ProductId)] : null;

                }

            }
            if (result.AdditionalLicenses == null)
                result.AdditionalLicenses = [];
            if (result.PrimaryLicenses == null)
                result.PrimaryLicenses = [];

            return result;
        }


        private async Task<List<AssetOwnerDetailViewDTO>> GetAssetOwners(string RecordGuid, CancellationToken cancellationToken)
        {
            var sql = $@"select 
                        ao.OwnerTypeId OwnerTypeId,
                        Case When ao.OwnerTypeId = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.NAME')
                             When ao.OwnerTypeId = 1 Then cd.ClubName
	                         Else CONCAT(u.FirstName, ' ', u.LastName)
                        End OwnerName,
                        Case When ao.OwnerTypeId = 0 Then null
                             When ao.OwnerTypeId = 1 Then CAST(cdd.SyncGuid as nvarchar(255))
	                         Else CAST(u.UserSyncId as nvarchar(255))
                        End OwnerId,
                        Case When ao.OwnerTypeId = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.LOGO')
                             When ao.OwnerTypeId = 1 Then cd.[Location]
	                         Else u.ProfilePicURL
                        End ProfileImage,
                        Case When ao.OwnerTypeId = 0 Then ''
                                When ao.OwnerTypeId = 1 Then cd.ClubId
	                            Else  u.MemberId
                        End OwnerReferenceId,
                        Case When ao.OwnerTypeId = 0 Then 0
                                When ao.OwnerTypeId = 1 Then cd.DocId
	                            Else u.Userid
                        End OwnerDocId,
                        Case When ao.OwnerTypeId = 0 Then ''
                                When ao.OwnerTypeId = 1 Then cd.ClubemailAddress
	                            Else ISNULL(u.EmailAddress,'')
                        End Email
                        from 
                        AssetOwners ao
                        Inner Join AssetRegisters ar on ar.AssetId = ao.AssetId
                        Left Join Clubs_Default cd on cd.DocId = ao.OwnerId and ao.OwnerTypeId = 1
                        Left Join Document cdd on cdd.DocId = cd.DocId
                        Left Join [User] u on u.Userid = ao.OwnerId and ao.OwnerTypeId = 2
                        WHERE ar.RecordGuid = @RecordGuid ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", RecordGuid);

            var result = (await _readRepository.GetLazyRepository<AssetOwnerDetailViewDTO>()
                     .Value.GetListAsync(sql, cancellationToken, queryParameters,
                         null, "text")).ToList();

            if (result.Any())
            {
                result[0].IsPrimary = true;
            }

            return result;
        }

        private async Task<List<AssetCredentialDTO>> GetAssetCredentials(string RecordGuid, CancellationToken cancellationToken)
        {

            string dataSql = $@"select 
                      ac.CredentialDocId as DocId,
					  ac.RecordGuid as AssetCredentialId,
                      cmd.Credentialname , 
                      cmd.ShortName, 
                      cmd.CredentialCode, 
                      S.AssetStatusId, 
                      S.name [StateName], 
                      cmd.Credentialcategory as [CredentialCategory],
					  --ac.StartDate,
					  --ac.EndDate as ExpiryDate,
					  D.SyncGuid  as MasterCredentialId,
					 -- case when DATEADD( second, x.gm_offset, [ac].StartDate) is null 
					 -- or DATEADD( second, x.gm_offset, [ac].StartDate) = '1900-01-01 00:00:00.000' 
					 -- then [ac].StartDate else DATEADD(  second, x.gm_offset,[ac].StartDate)end as StartDate
					 -- ,case when DATEADD( second, y.gm_offset, [ac].EndDate) is null 
						--or DATEADD( second, y.gm_offset, [ac].EndDate) = '1900-01-01 00:00:00.000' 
						--then [ac].EndDate else DATEADD(  second, y.gm_offset,[ac].EndDate) end as
						--ExpiryDate
						ac.StartDate
						,ac.EndDate as ExpiryDate
                      from 
                      AssetCredentials ac 
                      inner join AssetRegisters ar on ar.AssetId = ac.AssetId 
                      inner join credentialmaster_default cmd on cmd.DocId = ac.CredentialmasterDocId
					  inner join AssetStatus S on S.AssetStatusId =  ac.StatusId and s.Type = 3
					  inner join Document D on D.DocId = cmd.DocId
					  --		OUTER APPLY (
						 --    SELECT TOP 1 gm_offset, abbreviation 
						 --    FROM Timezone 
						 --    WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01 00:00:00',  [ac].StartDate) AS BIGINT) * 60 * 60
						 --      AND zone_id = 161
						 --    ORDER BY time_start DESC
						 --) AS X
						 --OUTER APPLY (
						 --    SELECT TOP 1 gm_offset, abbreviation 
						 --    FROM Timezone 
						 --    WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01 00:00:00', [ac].EndDate) AS BIGINT) * 60 * 60
						 --      AND zone_id = 161
						 --    ORDER BY time_start DESC
						 --) AS Y 
                      where 
                      ar.RecordGuid = @RecordGuid
					  order by ac.AssetCredentialId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", RecordGuid);

            return (await _readRepository.GetLazyRepository<AssetCredentialDTO>()
                     .Value.GetListAsync(dataSql, cancellationToken, queryParameters,
                         null, "text")).ToList();

        }

        private async Task<AssetDTOWithRawData> FetchRawData(GetSingleAssetQuery request, CancellationToken cancellationToken)
        {

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
            .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = {currentUserId}", cancellationToken, null, null, "text");


            string dataSql = $@"WITH 
                        Assets as (
                            SELECT DISTINCT
                                ar.RecordGuid AssetRegisterId,
                                ar.*,
                                ac.RecordGuid CategoryId,
                                ac.Name Category,
                                ast.Name AssetStatus,
                                ar.AssetId as  AssetDocCode
                                FROM [dbo].[AssetRegisters] ar
                                LEFT JOIN [dbo].[AssetCategories] ac on ac.AssetCategoryId = ar.AssetCategoryId
                                INNER JOIN [dbo].[AssetStatus] ast on ast.AssetStatusId = ar.StatusId and Type = 1
                                LEFT JOIN [dbo].[AssetTagLink] atl on atl.AssetId = ar.AssetId
                                LEFT JOIN [dbo].[AssetTypesTag] att on att.TagId = atl.TagId
                                Where ar.RecordGuid = @RecordGuid
                        ),
                        Tags as (
                            Select a.AssetId, STRING_AGG(att.Name, ',') Tags from 
                            AssetRegisters a
                            INNER JOIN [dbo].[AssetTagLink] atl on atl.AssetId = a.AssetId
                            INNER JOIN [dbo].[AssetTypesTag] att on att.TagId = atl.TagId
                            Where a.RecordGuid = @RecordGuid
                            GROUP BY a.AssetId
                        ),
                        ImageObjects as (
                            Select a.AssetId, 
                            CAST((
                                    SELECT 
                                        ai.AssetId AS AssetId,
                                        ai.AssetImageId AS AssetImageId,
			                            ai.RecordGuid AS ImageId,
			                            ai.AssetImage AS AssetImage,
			                            ai.IsPrimary AS IsPrimary
                                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                ) AS NVARCHAR(MAX)) ImageObj
                            from AssetRegisters a
                            INNER JOIN [dbo].[AssetImages] ai on ai.AssetId = a.AssetId
                            Where a.RecordGuid = @RecordGuid
                        ),
                        Images as (
                            Select io.AssetId, STRING_AGG(io.ImageObj, '||') Images
                            from ImageObjects io
                            GROUP BY io.AssetId
                        ),
                        PrimaryLicenses as (
                                SELECT 
                                    ar.AssetId AssetId,
                                        CAST((
                                            SELECT 
                                                ast.Name AS LicenseStatus,
                                                al.StartDate StartDate,
                                                al.EndDate AS EndDate,
                                                al.CancelEffectiveFrom CancelEffectiveFrom,
                                                al.RecordGuid AS AssetLicenseId,
                                                pd.[Name] AS [Name],
                                                ll.DocId AS LicenseId,
                                                ll.EntityId AS ProductId,
                                                pd.OwnerId AS OwnerId,
                                                atl.IsUpgradable
                                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                        ) AS NVARCHAR(MAX)) AS LicenseInfo
                                FROM 
                                    Products_Default pd
                                    INNER JOIN License_Links ll ON ll.Entityid = pd.DocId
                                    INNER JOIN AssetLicenses al ON al.ProductId = pd.DocId
                                    INNER JOIN AssetStatus ast ON ast.AssetStatusId = al.StatusId
                                    INNER JOIN AssetRegisters ar ON ar.AssetId = al.AssetId                               
                                    INNER JOIN AssetTypes at ON at.AssetTypeId = ar.AssetTypeId
                                    INNER JOIN AssetTypesLicenseLink atl 
                                        ON atl.AssetTypeId = ar.AssetTypeId AND atl.LicenseDocId = ll.DocId
                                   Where ar.RecordGuid = @RecordGuid and
                                    atl.LicenseType = 1
                        ),
                        PrimaryLicenseData as (
                           Select 
                             pl.AssetId,
                             STRING_AGG(LicenseInfo, '||') LicenseInfo
                            from PrimaryLicenses pl
                            Group By pl.AssetId
                        ),
                        AdditionalLicenses as (
                                SELECT 
                                    ar.AssetId AssetId,
                                        CAST((
                                            SELECT 
                                                ast.Name AS LicenseStatus,
                                                al.StartDate StartDate,
                                                al.EndDate AS EndDate,
                                                al.CancelEffectiveFrom CancelEffectiveFrom,
                                                al.RecordGuid AS AssetLicenseId,
                                                pd.[Name] AS [Name],
                                                ll.DocId AS LicenseId,
                                                ll.EntityId AS ProductId,
                                                pd.OwnerId AS OwnerId
                                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                        ) AS NVARCHAR(MAX)) AS LicenseInfo
                                FROM 
                                    Products_Default pd
                                    INNER JOIN License_Links ll ON ll.Entityid = pd.DocId
                                    INNER JOIN AssetLicenses al ON al.ProductId = pd.DocId
                                    INNER JOIN AssetStatus ast ON ast.AssetStatusId = al.StatusId
                                    INNER JOIN AssetRegisters ar ON ar.AssetId = al.AssetId                                   
                                    INNER JOIN AssetTypes at ON at.AssetTypeId = ar.AssetTypeId
                                    INNER JOIN AssetTypesLicenseLink atl 
                                        ON atl.AssetTypeId = ar.AssetTypeId AND atl.LicenseDocId = ll.DocId
                                Where ar.RecordGuid = @RecordGuid and 
                                    atl.LicenseType = 2
                        ),
                        AdditionalLicenseData as (
                           Select 
                             al.AssetId,
                             STRING_AGG(LicenseInfo, '||') LicenseInfo
                            from AdditionalLicenses al
                            Group By al.AssetId
                        )
                        Select a.*, i.Images, atg.Tags,
                        pld.LicenseInfo PrimaryLicenseInfo,
                        ald.LicenseInfo AdditionalLicenseInfo
                        FROM Assets a
                        LEFT JOIN PrimaryLicenseData pld on pld.AssetId = a.AssetId 
                        LEFT JOIN AdditionalLicenseData ald on ald.AssetId = a.AssetId 
                        LEFT JOIN Images i on i.AssetId = a.AssetId
                        LEFT JOIN Tags atg on atg.AssetId = a.AssetId
                        ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetRegisterId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                   select AssetTypeId from AssetRegisters where RecordGuid = @RecordGuid
               )", cancellationToken, queryParameters, null, "text");

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);

            if (!userGroups.Any(r => typeConfig.Permission.View.Contains(r.Value)))
            {
                return null;
            }

            return await _readRepository.GetLazyRepository<AssetDTOWithRawData>()
           .Value.GetAsync(dataSql, cancellationToken, queryParameters,
             null, "text");

        }

    }
}
