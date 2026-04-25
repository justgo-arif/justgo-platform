using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomCors;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
#else
using System.Configuration;
using System.Data.SqlClient;
#endif
using Microsoft.Extensions.Configuration;

namespace JustGo.Authentication.Infrastructure.CustomCors
{
    public class CorsOriginService : ICorsOriginService
    {
#if NET9_0_OR_GREATER
        private readonly IConfiguration _config;
#endif
        private readonly string _connectionString;
#if NET9_0_OR_GREATER
        public CorsOriginService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("ApiConnection");
        }
#else
        public CorsOriginService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ApiConnection"].ConnectionString;
        }
#endif

        public List<string> GetAllowedOrigins()
        {
            //need to implement caching here
            using (var connection = new SqlConnection(_connectionString))
            {
                //var sql = "SELECT DISTINCT [TenantDomainUrl] FROM [dbo].[Tenants]";
                var sql = @"SELECT DISTINCT LTRIM(RTRIM(s.value)) AS Origin
                            FROM [dbo].[Tenants] t
                            CROSS APPLY STRING_SPLIT(t.[TenantDomainUrl], ';') AS s;";
                var queryParameters = new DynamicParameters();
                var results = (connection.Query<string>(sql, queryParameters)).ToList();
                return results;
            }
        }
        public List<string> GetAllowedOriginsByOrigin(string origin)
        {
            //need to implement caching here
            using (var connection = new SqlConnection(_connectionString))
            {
                //var sql = "SELECT DISTINCT [TenantDomainUrl] FROM [dbo].[Tenants]";
                var sql = @"SELECT DISTINCT LTRIM(RTRIM(s.value)) AS Origin
                            FROM [dbo].[Tenants] t
                            CROSS APPLY STRING_SPLIT(t.[TenantDomainUrl], ';') AS s
                            WHERE LTRIM(RTRIM(s.value)) = @origin;";
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@origin", origin);
                var results = (connection.Query<string>(sql, queryParameters)).ToList();
                return results;
            }
        }
        public bool IsTenantOriginAllowed(string origin)
        {
            //need to implement caching here
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT CASE 
                                WHEN EXISTS (
                                    SELECT 1
                                    FROM [dbo].[Tenants] 
                                    CROSS APPLY STRING_SPLIT([TenantDomainUrl], ';') AS s
                                    WHERE LTRIM(RTRIM(s.value)) = @origin
                                )
                                THEN CAST(1 AS BIT)
                                ELSE CAST(0 AS BIT)
                            END AS IsOriginAllowed;";
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@origin", origin);
                var results = connection.ExecuteScalar<bool>(sql, queryParameters);
                return results;
            }
        }




    }
}
