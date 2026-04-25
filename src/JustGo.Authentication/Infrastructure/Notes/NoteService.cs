using Dapper;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Infrastructure.Notes
{
    public class NoteService : INoteService
    {
#if NET9_0_OR_GREATER
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUtilityService _utilityService;
        private readonly IDictionary<string, string> _providers;
        public NoteService(IReadRepositoryFactory readRepository, IWriteRepositoryFactory writeRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
            _providers = GetTableProviders();
        }

        public async Task<List<Note>> GetNotes(Guid entityId, int entityType, string module, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var entityIdInt = await GetEntityId(entityType, entityId, cancellationToken);
            var sql = $@"SELECT n.[NotesId]
                            ,n.[NotesGuid]
                            ,n.[EntityType]
                            ,n.[Details]
                            ,n.[UserId]
                            ,(select  [dbo].[GET_UTC_LOCAL_DATE_TIME] (n.[CreatedDate],0)) CreatedDate
	                        ,CONCAT(u.FirstName, ' ', u.LastName) UserName
	                        ,u.ProfilePicURL
                        FROM {tableName} n
	                        INNER JOIN [dbo].[User] u ON u.Userid=n.UserId
                        WHERE [EntityId]=@EntityId
                        AND [EntityType]=@EntityType";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityId", entityIdInt);
            queryParameters.Add("@EntityType", entityType);
            var results = (await _readRepository.GetLazyRepository<Note>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return results;
        }
        public async Task<PagedResult<Note>> GetNotes(Guid entityId, int entityType, string module, PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var entityIdInt = await GetEntityId(entityType, entityId, cancellationToken);
            var sql = $@"SELECT n.[NotesId]
                            ,n.[NotesGuid]
                            ,n.[EntityType]
                            ,n.[Details]
                            ,n.[UserId]
                            ,(select  [dbo].[GET_UTC_LOCAL_DATE_TIME] (n.[CreatedDate],0)) CreatedDate
	                        ,CONCAT(u.FirstName, ' ', u.LastName) UserName
	                        ,u.ProfilePicURL
                        FROM {tableName} n
	                        INNER JOIN [dbo].[User] u ON u.Userid=n.UserId
                        WHERE [EntityId]=@EntityId
                        AND [EntityType]=@EntityType
                        ORDER BY n.[NotesId]
                        OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;
                        SELECT COUNT(1) FROM {tableName}
                        WHERE [EntityId]=@EntityId
                        AND [EntityType]=@EntityType;";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityId", entityIdInt);
            queryParameters.Add("@EntityType", entityType);
            queryParameters.Add("@PageNumber", paginationParams.PageNumber);
            queryParameters.Add("@PageSize", paginationParams.PageSize);

            await using var multi = await _readRepository.GetLazyRepository<Note>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");

            var data = (await multi.ReadAsync<Note>()).ToList();
            var totalCount = await multi.ReadSingleAsync<int>();
            return new PagedResult<Note>
            {
                Items = data,
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }
        public async Task<KeysetPagedResult<Note>> GetNotes(Guid entityId, int entityType, string module, KeysetPaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var entityIdInt = await GetEntityId(entityType, entityId, cancellationToken);
            var sql = $@"SELECT TOP (@PageSizePlusOne) n.[NotesId]
                            ,n.[NotesGuid]
                            ,n.[EntityType]
                            ,n.[Details]
                            ,n.[UserId]
                            ,(select  [dbo].[GET_UTC_LOCAL_DATE_TIME] (n.[CreatedDate],0)) CreatedDate
	                        ,CONCAT(u.FirstName, ' ', u.LastName) UserName
	                        ,u.ProfilePicURL
                        FROM {tableName} n
	                        INNER JOIN [dbo].[User] u ON u.Userid=n.UserId
                        WHERE [EntityId]=@EntityId
                            AND [EntityType]=@EntityType
                            AND (@LastSeenId IS NULL OR n.[NotesId] > @LastSeenId)
                        ORDER BY n.[NotesId];";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityId", entityIdInt);
            queryParameters.Add("@EntityType", entityType);
            queryParameters.Add("@LastSeenId", paginationParams.LastSeenId);
            queryParameters.Add("@PageSizePlusOne", paginationParams.PageSize + 1);

            var data = (await _readRepository.GetLazyRepository<Note>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            var hasMore = data.Count > paginationParams.PageSize;
            if (hasMore)
                data.RemoveAt(data.Count - 1);

            queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityId", entityIdInt);
            queryParameters.Add("@EntityType", entityType);

            sql = $@"SELECT COUNT(1) FROM {tableName}
                        WHERE [EntityId]=@EntityId
                        AND [EntityType]=@EntityType;";
            var totalCount = Convert.ToInt32(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text"));
            //totalcount need to cache
            return new KeysetPagedResult<Note>
            {
                Items = data,
                TotalCount = totalCount,
                HasMore = hasMore,
                LastSeenId = data.Count > 0 ? data[data.Count - 1].NotesId : null
            };
        }
        public async Task<int> CreateNote(Note note, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(note.Module);
            var entityId = await GetEntityId(note.EntityType, note.EntityId, cancellationToken);
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var sql = $@"INSERT INTO {tableName}
                           ([EntityType]
                           ,[EntityId]
                           ,[Details]
                           ,[UserId]
                           ,[CreatedDate])
                     VALUES
                           (@EntityType
                           ,@EntityId
                           ,@Details
                           ,@UserId
                           ,@CreatedDate)";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityType", note.EntityType);
            queryParameters.Add("@EntityId", entityId);
            queryParameters.Add("@Details", note.Details);
            queryParameters.Add("@UserId", currentUserId);
            queryParameters.Add("@CreatedDate", DateTime.UtcNow);
            return await _writeRepository.GetLazyRepository<Note>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
        }
        public async Task<int> EditNote(Note note, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(note.Module);
            var entityId = await GetEntityId(note.EntityType, note.EntityId, cancellationToken);
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var sql = $@"UPDATE {tableName}
                       SET [EntityType] = @EntityType
                          ,[EntityId] = @EntityId
                          ,[Details] = @Details
                          ,[UserId] = @UserId
                          ,[CreatedDate] = @CreatedDate
                     WHERE [NotesGuid]=@NotesGuid";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityType", note.EntityType);
            queryParameters.Add("@EntityId", entityId);
            queryParameters.Add("@Details", note.Details);
            queryParameters.Add("@UserId", currentUserId);
            queryParameters.Add("@CreatedDate", DateTime.UtcNow);
            queryParameters.Add("@NotesGuid", note.NotesGuid);
            return await _writeRepository.GetLazyRepository<Note>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
        }
        public async Task<int> DeleteNote(Guid id, string module, CancellationToken cancellationToken)
        {
            var tableName = GetTableName(module);
            var sql = $@"DELETE FROM {tableName}
                            WHERE [NotesGuid]=@NotesGuid";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@NotesGuid", id);
            return await _writeRepository.GetLazyRepository<Note>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
        }

        private IDictionary<string, string> GetTableProviders()
        {
            var providers = new Dictionary<string, string>
            {
               { "Member", "[dbo].[MemberNotes]" },
               { "Asset", "[dbo].[AssetNotes]" },
               { "Finance", "[dbo].[FinanceNotes]" }
            };
            return providers;
        }
        private string GetTableName(string module)
        {
            if (_providers.TryGetValue(module, out var table))
                return table;
            throw new NotFoundException($"No notes table found for the module: {module}");
        }
        private async Task<int> GetEntityId(int entityType, Guid entityGuid, CancellationToken cancellationToken)
        {
            var query = @"SELECT [NotesEntityTypeMappingId]
                              ,[EntityTypeId]
                              ,[EntityTypeName]
                              ,[TableName]
                              ,[GuidColumn]
                              ,[IdColumn]
                        FROM [dbo].[NotesEntityTypeMappings]
                        WHERE [EntityTypeId]=@EntityTypeId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityTypeId", entityType);
            var mapping = await _readRepository.GetLazyRepository<NotesEntityTypeMapping>().Value.GetAsync(query, cancellationToken, queryParameters, null, "text");
            if (mapping is null)
            {
                throw new NotFoundException($"No mapping found for the entity type: {entityType}");
            }
            if (string.IsNullOrWhiteSpace(mapping.TableName) || string.IsNullOrWhiteSpace(mapping.GuidColumn) || string.IsNullOrWhiteSpace(mapping.IdColumn))
            {
                throw new NotFoundException($"No table or Guid column or Id column found for the entity type: {entityType}");
            }

            var sql = $"SELECT {mapping.IdColumn} FROM {mapping.TableName} WHERE {mapping.GuidColumn}=@entityGuid";
            queryParameters = new DynamicParameters();
            queryParameters.Add("@entityGuid", entityGuid);
            var result = Convert.ToInt32(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text"));
            return result;
        }


#endif
    }
}
