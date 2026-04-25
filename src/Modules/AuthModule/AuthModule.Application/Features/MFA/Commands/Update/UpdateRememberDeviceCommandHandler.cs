using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Commands.Update;

public class UpdateRememberDeviceCommandHandler : IRequestHandler<UpdateRememberDeviceCommand, bool>
{
    private readonly LazyService<IWriteRepository<object>> _writeRepository;

    public UpdateRememberDeviceCommandHandler(LazyService<IWriteRepository<object>> writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<bool> Handle(UpdateRememberDeviceCommand request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        //if (request.IsAdmin)
        //{
        //    sql = @"update MFARememberedDevices set IsRememberEnabled = 0 where UserId = (select top 1 userId from [user] where MemberDocId = @MemberDocId) and IsRememberEnabled = 1";
        //    queryParameters.Add("@MemberDocId", request.MemberDocId);
        //}
        //else
        //{
        //    sql = @"update MFARememberedDevices set IsRememberEnabled = 0 where UserId = @UserId and IsRememberEnabled = 1";
        //    queryParameters.Add("@UserId", request.UserId);
        //}
        string sql = @"update MFARememberedDevices set IsRememberEnabled = 0 where UserId = @UserId and IsRememberEnabled = 1";
        queryParameters.Add("@UserId", request.UserId);

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        return result >= 0;
    }
}
