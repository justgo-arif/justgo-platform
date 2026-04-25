using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue
{
    public class IsActionAllowedQueryHandler : IRequestHandler<IsActionAllowedQuery, bool>
    {
        private readonly LazyService<IWriteRepository<string>> _readRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IsActionAllowedQueryHandler(LazyService<IWriteRepository<string>> readRepository, IHttpContextAccessor httpContextAccessor)
        {
            _readRepository = readRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Handle(IsActionAllowedQuery request, CancellationToken cancellationToken)
        {
            if (IsReactDevelopment()) return true;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@InvokingUserId", request.InvokingUserId);
            queryParameters.Add("@DocId", request.MemberDocId);
            queryParameters.Add("@Argument", request.Option);
            queryParameters.Add("@Result", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

            // Execute the stored procedure
            await _readRepository.Value.ExecuteAsync("IsActionAllowed", queryParameters, null, "sp");

            // Retrieve the output value
            string result = queryParameters.Get<string>("@Result");

            return string.IsNullOrEmpty(result);
        }

        private bool IsReactDevelopment()
        {
            return !string.IsNullOrEmpty(_httpContextAccessor.HttpContext.Request.Headers["x-react-header"].ToString());
        }
    }
}
