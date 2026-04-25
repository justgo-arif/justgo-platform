using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json.Linq;

namespace AuthModule.Application.Features.MFA.Commands.Create;

public class SaveMFALogCommandHandler : IRequestHandler<SaveMFALogCommand, bool>
{
    private readonly LazyService<IWriteRepository<JustGoMFA_Log>> _writeRepository;

    public SaveMFALogCommandHandler(LazyService<IWriteRepository<JustGoMFA_Log>> writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<bool> Handle(SaveMFALogCommand request, CancellationToken cancellationToken)
    {
        string sql = @"INSERT INTO JustGoMFA_Log (UserId,[Type],ParametersJson,Details,Date,[Action])
                        select @UserId,@Type,@ParametersJson,@Result,GETDATE(),@Action";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", request.UserId);
        queryParameters.Add("@Type", request.Type);
        queryParameters.Add("@Result", JObject.FromObject(request.Obj).ToString());
        queryParameters.Add("@Action", request.Action);
        queryParameters.Add("@ParametersJson", JObject.FromObject(request.Args).ToString());

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        return result >= 0;
    }
}
