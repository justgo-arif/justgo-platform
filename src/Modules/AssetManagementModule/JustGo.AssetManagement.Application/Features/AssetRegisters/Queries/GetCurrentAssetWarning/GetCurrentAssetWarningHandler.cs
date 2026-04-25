using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Linq;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetCurrentAssetWarning
{
    public class GetCurrentAssetWarningHandler : IRequestHandler<GetCurrentAssetWarningQuery, AssetNotificationModel>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetCurrentAssetWarningHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<AssetNotificationModel> Handle(GetCurrentAssetWarningQuery request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetRegisterId);
            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                 .GetAsync($@"Select * from AssetRegisters Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", asset.AssetId);
            var reasons = (await _readRepository.GetLazyRepository<SelectListItemDTO<string>>().Value
                 .GetListAsync($@"Select [Reason] [Text], [Reason] [Value] from GetActionRequiredByAssetId(@AssetId)", cancellationToken, queryParameters, null, "text")).ToList();

            var retentionMessage = await GetRetentionMessage(asset.AssetId, cancellationToken);



            return new AssetNotificationModel()
            {
                BasicDetails = (await GetBasicDetailsWarning(request, cancellationToken))
                               ? new SectionNotification { Type = "warning", Message = "Basic details requires attention." }
                               : null,
                AdditionalDetails = (await GetAdditionalDetailsWarning(request, cancellationToken))
                               ? new SectionNotification { Type = "warning", Message = "Some additional information is missing." }
                               : null,
                Credential = reasons.Any(x => x.Value.ToLower().Contains("certificate")) ? new SectionNotification { Type = "warning", Message = "Certificates require attention." } : null,
                License = reasons.Any(x => x.Value.ToLower().Contains("license")) ? new SectionNotification { Type = "warning", Message = "Licences require attention." } : null,
                Lease = reasons.Any(x => x.Value.ToLower().Contains("lease")) ? new SectionNotification { Type = "warning", Message = "Leases require attention." } : null,
                Transfer = reasons.Any(x => x.Value.ToLower().Contains("transfer")) ? new SectionNotification { Type = "warning", Message = "Ownership require attention." } : null,
                Retention = retentionMessage != null ? new SectionNotification { Type = "warning", Message = retentionMessage } : null


            };
        }

        public async Task<string> GetRetentionMessage(int  assetId,
                                               CancellationToken cancellationToken)
        {

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", assetId);

            var retensionCheckSql = $@" DECLARE @assetTypeId INT = (select AssetTypeId from AssetRegisters where AssetId = @AssetId);

                                        DECLARE @retentionConfig NVARCHAR(MAX) = (
                                            SELECT ast.AssetRetentionConfig
                                            FROM AssetTypes ast
                                            WHERE ast.AssetTypeId = @assetTypeId
                                        );



                                        DECLARE @sql NVARCHAR(MAX) = N'
                                        SELECT DISTINCT 
                                        ao.OwnerId [Value], concat(u.FirstName, '' '', u.LastName) [Text]
                                        FROM [AssetOwners] ao
                                        inner join [user] u on u.Userid = ao.OwnerId AND ao.OwnerTypeId = 2 
                                        INNER JOIN [AssetRegisters] ar ON ar.AssetId = ao.AssetId AND ar.AssetTypeId = @assetTypeId and ar.AssetId = @AssetId
                                        left JOIN UserMemberships um ON um.userid = ao.OwnerId AND ao.OwnerTypeId = 2 AND um.StatusId = 62
                                        ';


                                        DECLARE @joins NVARCHAR(MAX) = N'';
                                        DECLARE @conditions NVARCHAR(MAX) = ' ';

                                        IF (@retentionConfig IS NOT NULL AND @retentionConfig != '')
                                        BEGIN
                                            DECLARE @i INT = 0;
                                            DECLARE @max INT;
                                            SELECT @max = COUNT(*) FROM OPENJSON(@retentionConfig);

                                            WHILE @i < @max
                                            BEGIN
                                                DECLARE @memberships NVARCHAR(MAX);
                                                DECLARE @operation NVARCHAR(10);
		                                        DECLARE @bracket NVARCHAR(10);

                                                SELECT 
                                                    @memberships = obj.Memberships,
                                                    @operation = COALESCE(NULLIF(TRIM(obj.Operation), ''), ''),
			                                        @bracket = COALESCE(NULLIF(TRIM(obj.Bracket), ''), '')
                                                FROM OPENJSON(@retentionConfig) o
                                                CROSS APPLY OPENJSON(o.value) WITH (
                                                        Memberships NVARCHAR(MAX) '$.Memberships',
                                                        Operation NVARCHAR(10) '$.Operation',
				                                        Bracket NVARCHAR(10) '$.Bracket'
                                                    ) AS obj
                                                WHERE o.[key] = CAST(@i AS NVARCHAR)
		                                        ;

                                                IF (@memberships IS NOT NULL AND @memberships != '')
                                                BEGIN
                                                    SET @joins = @joins + '
                                        LEFT JOIN Products_Links pl_' + CAST(@i AS NVARCHAR) + ' ON pl_' + CAST(@i AS NVARCHAR) + '.DocId = um.ProductId AND pl_' + CAST(@i AS NVARCHAR) + '.EntityId IN (' + @memberships + ')';

                                                    SET @conditions = @conditions + ' '	+			  
			                                              (
				                                            case when @bracket like '%(%' then @bracket else '' end
				                                          )+
				                                          ' max(isnull(pl_' + CAST(@i AS NVARCHAR) + '.DocId, 0)) = 0'
				                                          + 
				                                          (
				                                           case when @bracket like '%)%' then @bracket else '' end
				                                          )+
				                                          ' '+
				                                          (
				                                             case when @operation like 'and' then 'or' 
					                                              when @operation like 'or' then 'and' 
						                                          else ''
					                                         end
		                                                  );
                                                END

                                                SET @i = @i + 1;
                                            END


                                            SET @sql = @sql + @joins + N'
                                            group by  ao.OwnerId, u.FirstName, u.LastName having ' + ' (' + @conditions + ')';

                                            PRINT @sql;

                                            EXEC sp_executesql 
                                                @sql,
                                                N'@assetTypeId INT, @assetId INT',
                                                @assetTypeId = @assetTypeId,
                                                @assetId = @assetId;

                                         END
                                         Else 
                                         Begin
                                             select 0 [Value], '' [Text] where 1=0
                                         End


                                        ";

            var retensionOwners = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value
                                    .GetListAsync(retensionCheckSql, cancellationToken, queryParameters, null, "text")).ToList();

            if (retensionOwners.Any())
            {

                var lastViewOwnerId = retensionOwners.Count() > 1 ?
                                     retensionOwners.FirstOrDefault().Value :
                                     0;

                return string.Join(",", retensionOwners.Where(r => r.Value != lastViewOwnerId).Select(r => r.Text)) + 
                       (lastViewOwnerId != 0 ? " and " + retensionOwners.FirstOrDefault(r => r.Value == lastViewOwnerId).Text : "") +
                       (lastViewOwnerId != 0 ? " are " : " is ") +
                       "missing required membership(s).";
            }


            return null;


        }

        public async Task<bool> GetBasicDetailsWarning(GetCurrentAssetWarningQuery request,
                                                       CancellationToken cancellationToken)
        {

            var sql = $@"SELECT 
                r.AssetName [Text],
                CASE 
                    WHEN EXISTS (
                        -- AssetCategoryId
                        SELECT 1
                        WHERE (r.AssetCategoryId IS NULL)
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'CategoryId'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetReference
                        SELECT 1
                        WHERE (r.AssetReference IS NULL OR LTRIM(RTRIM(r.AssetReference)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetReference'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetName
                        SELECT 1
                        WHERE (r.AssetName IS NULL OR LTRIM(RTRIM(r.AssetName)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetName'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetDescription
                        SELECT 1
                        WHERE (r.AssetDescription IS NULL OR LTRIM(RTRIM(r.AssetDescription)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetDescription'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Address1
                        SELECT 1
                        WHERE (r.Address1 IS NULL OR LTRIM(RTRIM(r.Address1)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Address1'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Address2
                        SELECT 1
                        WHERE (r.Address2 IS NULL OR LTRIM(RTRIM(r.Address2)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Address2'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- ManufactureDate
                        SELECT 1
                        WHERE (r.ManufactureDate IS NULL)
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'ManufactureDate'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Brand
                        SELECT 1
                        WHERE (r.Brand IS NULL OR LTRIM(RTRIM(r.Brand)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Brand'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- SerialNo
                        SELECT 1
                        WHERE (r.SerialNo IS NULL OR LTRIM(RTRIM(r.SerialNo)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'SerialNo'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Group
                        SELECT 1
                        WHERE (r.[Group] IS NULL OR LTRIM(RTRIM(r.[Group])) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Group'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetValue
                        SELECT 1
                        WHERE (r.AssetValue IS NULL OR LTRIM(RTRIM(r.AssetValue)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetValue'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetConfig
                        SELECT 1
                        WHERE (r.AssetConfig IS NULL OR LTRIM(RTRIM(r.AssetConfig)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetConfig'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Country
                        SELECT 1
                        WHERE (r.Country IS NULL OR LTRIM(RTRIM(r.Country)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Country'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Town
                        SELECT 1
                        WHERE (r.Town IS NULL OR LTRIM(RTRIM(r.Town)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Town'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- County
                        SELECT 1
                        WHERE (r.County IS NULL OR LTRIM(RTRIM(r.County)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'County'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- PostCode
                        SELECT 1
                        WHERE (r.PostCode IS NULL OR LTRIM(RTRIM(r.PostCode)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'PostCode'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- Barcode
                        SELECT 1
                        WHERE (r.Barcode IS NULL OR LTRIM(RTRIM(r.Barcode)) = '')
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'Barcode'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetImages
                        SELECT 1
                        WHERE NOT EXISTS (
                                SELECT 1
                                FROM AssetImages ai
                                WHERE ai.AssetId = r.AssetId
                          )
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetImages'
                          )
                    ) THEN 1

                    WHEN EXISTS (
                        -- AssetTagLink
                        SELECT 1
                        WHERE NOT EXISTS (
                                SELECT 1
                                FROM AssetTagLink atl
                                WHERE atl.AssetId = r.AssetId
                          )
                          AND NOT EXISTS (
                                SELECT 1
                                FROM OPENJSON(JSON_QUERY(t.AssetRegistrationConfig, '$.Steps.BasicDetail.Config.OptionalFields')) j
                                WHERE j.value = 'AssetTags'
                          )
                    ) THEN 1

                    ELSE 0
                END AS [Value]
            FROM AssetRegisters r
            INNER JOIN AssetTypes t ON r.AssetTypeId = t.AssetTypeId
            Where r.RecordGuid = @RecordGuid
            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetRegisterId);
            var reasons = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value
                        .GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();


            return reasons.Any(x => x.Value == 1);

        }


        public async Task<bool> GetAdditionalDetailsWarning(GetCurrentAssetWarningQuery request,
                                                       CancellationToken cancellationToken)
        {

            var sql = $@"declare @assetId int = (Select AssetId FROM AssetRegisters r Where r.RecordGuid = @RecordGuid);
                         select  top(1) 1 [Value], '' [Text] from  GetMissingAdditionalFieldForAsset(@assetId)
                        ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetRegisterId);
            var reasons = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value
                        .GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();


            return reasons.Any(x => x.Value == 1);

        }


    }
}
