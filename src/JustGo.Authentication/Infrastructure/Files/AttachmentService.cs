using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.Files;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Infrastructure.Files
{
    public class AttachmentService : IAttachmentService
    {
#if NET9_0_OR_GREATER
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUtilityService _utilityService;
        public AttachmentService(IReadRepositoryFactory readRepository, IWriteRepositoryFactory writeRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }

        public async Task<List<Attachment>> GetAttachments(Guid entityId, int entityType, string module, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var entityIdInt = await GetEntityId(entityType, entityId, cancellationToken);
            var sql = $@"
            SELECT A.AttachmentId, A.AttachmentGuid, A.EntityType, A.[Name], A.[Size], A.UserId,
            (SELECT [dbo].[GET_UTC_LOCAL_DATE_TIME] (A.CreatedDate, 0)) CreatedDate,
            CONCAT(U.FirstName, ' ', U.LastName) UserName, U.ProfilePicURL
            FROM {tableName} A
            INNER JOIN [User] U ON U.Userid = A.UserId
            WHERE A.EntityId = @EntityId
            AND A.EntityType = @EntityType";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("EntityId", entityIdInt);
            queryParameters.Add("EntityType", entityType);
            return (await _readRepository.GetLazyRepository<Attachment>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        }

        public async Task<PagedResult<Attachment>> GetAttachments(Guid entityId, int entityType, string module, PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var entityIdInt = await GetEntityId(entityType, entityId, cancellationToken);
            var sql = $@"
                        SELECT A.AttachmentId, A.AttachmentGuid, A.EntityType, A.[Name], A.[Size], A.UserId,
                        (SELECT [dbo].[GET_UTC_LOCAL_DATE_TIME] (A.CreatedDate, 0)) CreatedDate,
                        CONCAT(U.FirstName, ' ', U.LastName) UserName, U.ProfilePicURL
                        FROM {tableName} A
	                    INNER JOIN [User] u ON u.Userid = A.UserId
                        WHERE A.EntityId = @EntityId
                        AND A.EntityType = @EntityType
                        ORDER BY A.AttachmentId
                        OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(1) FROM {tableName}
                        WHERE EntityId = @EntityId
                        AND EntityType = @EntityType;
                        ";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("EntityId", entityIdInt);
            queryParameters.Add("EntityType", entityType);
            queryParameters.Add("PageNumber", paginationParams.PageNumber);
            queryParameters.Add("PageSize", paginationParams.PageSize);

            await using var multi = await _readRepository.GetLazyRepository<Attachment>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");

            var data = (await multi.ReadAsync<Attachment>()).ToList();
            var totalCount = await multi.ReadSingleAsync<int>();
            return new PagedResult<Attachment>
            {
                Items = data,
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<KeysetPagedResult<Attachment>> GetAttachments(Guid entityId, int entityType, string module, KeysetPaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var entityIdInt = await GetEntityId(entityType, entityId, cancellationToken);
            var sql = $@"
                    SELECT TOP (@PageSizePlusOne) A.AttachmentId, A.AttachmentGuid, A.EntityType, A.[Name], A.[Size], A.UserId,
                    (SELECT [dbo].[GET_UTC_LOCAL_DATE_TIME] (A.CreatedDate, 0)) CreatedDate,
                    CONCAT(U.FirstName, ' ', U.LastName) UserName, U.ProfilePicURL
                    FROM {tableName} A
                    INNER JOIN [dbo].[User] u ON u.Userid = A.UserId
                    WHERE A.EntityId = @EntityId
                    AND A.EntityType = @EntityType
                    AND (@LastSeenId IS NULL OR A.AttachmentId > @LastSeenId)
                    ORDER BY A.AttachmentId;

                    SELECT COUNT(1) FROM {tableName}
                    WHERE EntityId = @EntityId
                    AND EntityType = @EntityType;
                    ";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("EntityId", entityIdInt);
            queryParameters.Add("EntityType", entityType);
            queryParameters.Add("LastSeenId", paginationParams.LastSeenId);
            queryParameters.Add("PageSizePlusOne", paginationParams.PageSize + 1);

            await using var multi = await _readRepository.GetLazyRepository<Attachment>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");

            var data = (await multi.ReadAsync<Attachment>()).ToList();
            var totalCount = await multi.ReadSingleAsync<int>();

            var hasMore = data.Count > paginationParams.PageSize;
            if (hasMore)
                data.RemoveAt(data.Count - 1);

            return new KeysetPagedResult<Attachment>
            {
                Items = data,
                TotalCount = totalCount,
                HasMore = hasMore,
                LastSeenId = data.Count > 0 ? data[data.Count - 1].AttachmentId : 0
            };
        }

        public async Task<int> CreateAttachment(Attachment note, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(note.Module);
            var entityId = await GetEntityId(note.EntityType, note.EntityId, cancellationToken);
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var sql = $@"INSERT INTO {tableName} (EntityType, EntityId, [Name], GeneratedName, [Size], UserId, CreatedDate)
                                        VALUES (@EntityType, @EntityId, @Name, @GeneratedName, @Size, @UserId, @CreatedDate)";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("EntityType", note.EntityType);
            queryParameters.Add("EntityId", entityId);
            queryParameters.Add("Name", note.Name);
            queryParameters.Add("GeneratedName", note.GeneratedName);
            queryParameters.Add("Size", note.Size);
            queryParameters.Add("UserId", currentUserId);
            queryParameters.Add("CreatedDate", DateTime.UtcNow);
            return await _writeRepository.GetLazyRepository<Attachment>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, transaction, "text");
        }

        public async Task<int> EditAttachment(Attachment note, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(note.Module);
            var entityId = await GetEntityId(note.EntityType, note.EntityId, cancellationToken);
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var sql = $@"UPDATE {tableName}
                       SET EntityType = @EntityType
                          ,EntityId = @EntityId
                          ,[Name] = @Name
                          ,GeneratedName = @GeneratedName
                          ,[Size] = @Size
                          ,UserId = @UserId
                          ,CreatedDate = @CreatedDate
                     WHERE AttachmentGuid = @AttachmentGuid";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("EntityType", note.EntityType);
            queryParameters.Add("EntityId", entityId);
            queryParameters.Add("Name", note.Name);
            queryParameters.Add("UserId", currentUserId);
            queryParameters.Add("CreatedDate", DateTime.UtcNow);
            queryParameters.Add("AttachmentGuid", note.AttachmentGuid);
            return await _writeRepository.GetLazyRepository<Attachment>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, transaction, "text");
        }

        public async Task<int> DeleteAttachment(Guid id, string module, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var sql = $@"DELETE FROM {tableName}
                            WHERE AttachmentGuid = @AttachmentGuid";

            return await _writeRepository.GetLazyRepository<Attachment>().Value.ExecuteAsync(sql, cancellationToken, new { AttachmentGuid = id }, transaction, "text");
        }

        public async Task<Attachment> GetAttachment(Guid attachmentGuid, string module, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var sql = $@"
            SELECT A.AttachmentId, A.AttachmentGuid, A.EntityType, A.[Name], A.GeneratedName, A.[Size], A.UserId, A.CreatedDate
            FROM {tableName} A
            WHERE A.AttachmentGuid = @AttachmentGuid";

            var attachment = await _readRepository.GetLazyRepository<Attachment>().Value.GetAsync(sql, cancellationToken, new { AttachmentGuid = attachmentGuid }, null, "text");
            if (attachment is null)
            {
                throw new NotFoundException($"Attachment with GUID '{attachmentGuid}' not found in module '{module}'.");
            }
            return attachment;
        }

        private static readonly Dictionary<string, string> Tables = new Dictionary<string, string>
        {
            { "Member", "MemberAttachments" },
            { "Asset", "AssetAttachments" },
            { "Finance", "FinanceAttachments" }
        };

        private string GetTableName(string module)
        {
            if (Tables.TryGetValue(module, out var table))
                return table;
            throw new NotFoundException($"No attachments table found for the module: {module}");
        }

        private async Task<int> GetEntityId(int entityType, Guid entityGuid, CancellationToken cancellationToken)
        {
            var query = @"SELECT MappingId, EntityTypeId, EntityTypeName, TableName, GuidColumn, IdColumn
                        FROM AttachmentsEntityTypeMappings
                        WHERE EntityTypeId = @EntityTypeId";
            var mapping = await _readRepository.GetLazyRepository<AttachmentsEntityTypeMapping>().Value.GetAsync(query, cancellationToken, new { EntityTypeId = entityType }, null, "text");
            if (mapping is null)
            {
                throw new NotFoundException($"No mapping found for the entity type: {entityType}");
            }
            if (string.IsNullOrWhiteSpace(mapping.TableName) || string.IsNullOrWhiteSpace(mapping.GuidColumn) || string.IsNullOrWhiteSpace(mapping.IdColumn))
            {
                throw new NotFoundException($"No table or Guid column or Id column found for the entity type: {entityType}");
            }

            var sql = $"SELECT {mapping.IdColumn} FROM {mapping.TableName} WHERE {mapping.GuidColumn}=@entityGuid";
            var result = Convert.ToInt32(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, new { entityGuid }, null, "text"));
            return result;
        }
#endif
    }
}
