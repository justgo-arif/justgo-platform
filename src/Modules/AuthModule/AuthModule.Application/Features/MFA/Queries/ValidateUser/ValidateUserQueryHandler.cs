using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace AuthModule.Application.Features.MFA.Queries.ValidateUser;

public class ValidateUserQueryHandler : IRequestHandler<ValidateUserQuery, bool>
{
    private readonly LazyService<IReadRepository<User>> _readRepository;
    private readonly IUtilityService _utilityService;
    public ValidateUserQueryHandler(LazyService<IReadRepository<User>> readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }

    public async Task<bool> Handle(ValidateUserQuery request, CancellationToken cancellationToken)
    {
        string loginId = string.Empty;
        bool IsValid;
        string pass = string.Empty;
        bool IsLocked = false;
        bool IsActive = true;

        string sql = @"Select Password, IsLocked, IsActive from [User] where LoginId=@LoginId and Password=@Password";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@LoginId", request.UserName);
        queryParameters.Add("@Password", _utilityService.Encrypt(request.Password));

        var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");

        if (result != null)
        {
            loginId = request.UserName;
            pass = result.Password;
            IsLocked = result.IsLocked;
            IsActive = result.IsActive;
        }

        //if (result == null || string.IsNullOrWhiteSpace(result.Password.ToString()))
        //{
        //    string sqlMember = @"Select u.LoginId, u.[Password], u.IsLocked, u.IsActive from [User] u
        //                            inner join EntityLink el on el.SourceId = u.Userid and u.[Password] = @Password
        //                            inner join Members_Default md on md.DocId = el.LinkId and md.MID = @MID";

        //    var queryParametersMid = new DynamicParameters();
        //    queryParametersMid.Add("@MID", request.UserName);
        //    queryParametersMid.Add("@Password", _utilityService.Encrypt(request.Password));

        //    var resultMid = await _readRepository.Value.GetSingleAsync(sqlMember, queryParametersMid, null, "text");

        //    var dataMid = JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(result));

        //    if (dataMid != null)
        //    {
        //        loginId = dataMid["LoginId"].ToString();
        //        pass = dataMid["Password"].ToString();
        //        IsLocked = (bool)dataMid["IsLocked"];
        //        IsActive = (bool)dataMid["IsActive"];
        //    }
        //}

        if (!string.IsNullOrWhiteSpace(loginId) && loginId.ToLower() != "admin")
        {
            if (!IsActive)
                throw new Exception("User is not active");
            else if (IsLocked)
                throw new Exception("User Locked");
        }
        IsValid = pass == _utilityService.Encrypt(request.Password);

        return IsValid;
    }
}
