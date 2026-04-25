using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;


namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveOptinCurrent
{
    public class SaveOptinCurrentHandler : IRequestHandler<SaveOptinCurrentCommand, string>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUtilityService _utilityService;

        public SaveOptinCurrentHandler(IWriteRepositoryFactory writeRepositoryFactory, IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _utilityService = utilityService;
        }

        public string INSERT_SQL =
           """
            MERGE dbo.OptinCurrent WITH (HOLDLOCK) AS target
            USING(SELECT @EntityId AS EntityId, @OptInId AS OptInId, @OptinValue AS [Value], @ActionUserId AS ActionUserId) AS source
            ON target.EntityId = source.EntityId
            AND target.OptInId = source.OptInId
            
            WHEN MATCHED THEN
                UPDATE SET[Value] = source.[Value], [Version] = target.[Version] + 1, LastModifiedUser = source.ActionUserId, ActionDate = SYSUTCDATETIME()
            
            WHEN NOT MATCHED THEN
                INSERT (EntityId, OptInId, [Value], [Version], LastModifiedUser, ActionDate)
                VALUES (source.EntityId, source.OptInId, source.[Value], 1, source.ActionUserId, SYSUTCDATETIME())
            
            OUTPUT
                inserted.Id,
                deleted.[Value],
                inserted.[Value],
                inserted.LastModifiedUser,
                inserted.ActionDate
            INTO OptInHistory(OptinCurrentId, OldValue, NewValue, ActionUser, ActionDate);
            """;
        public async Task<string> Handle(SaveOptinCurrentCommand request, CancellationToken cancellationToken = default)
        {

            var parameters = new DynamicParameters();
            parameters.Add("@EntityId", request.EntityId, System.Data.DbType.Int32);
            parameters.Add("@OptInId", request.OptinId, System.Data.DbType.Int32);
            parameters.Add("@ActionUserId", request.ActionUserId, System.Data.DbType.Int32);
            parameters.Add("@OptinValue", request.OptinValue , System.Data.DbType.String);

            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            await repo.ExecuteAsync(
               INSERT_SQL,
               cancellationToken,
               parameters,
               dbTransaction: null,
               commandType: "text"
           );

            return "Success";

        }
    }
}
