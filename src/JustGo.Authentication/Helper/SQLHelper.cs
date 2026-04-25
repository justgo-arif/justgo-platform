using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;

namespace JustGo.Authentication.Helper
{
    public static class SQLHelper
    {
        public static (string sqlQuery, DynamicParameters queryParameters) GenerateInsertSQLWithParameters<T>(T model, string[] fieldsToSkip, string tableName) where T : class
        {
            Dictionary<string, dynamic> keyValuePairs =
                 JsonConvert.DeserializeObject<Dictionary<string, dynamic>>
                 (JsonConvert.SerializeObject(model));

            keyValuePairs = keyValuePairs.Where(r => !fieldsToSkip.Contains(r.Key)).ToDictionary(r => r.Key, r => r.Value);

            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append($@"INSERT INTO {tableName} ( ");
            sqlBuilder.Append(string.Join(",", keyValuePairs.Select(item => $"[{item.Key}]")));
            sqlBuilder.Append(" ) VALUES ( ");
            sqlBuilder.Append(string.Join(",", keyValuePairs.Select(item => $"@{item.Key}")));
            sqlBuilder.Append(" ); SELECT SCOPE_IDENTITY() AS Id;");

            var sql = sqlBuilder.ToString();

            var queryParameters = new DynamicParameters();
            foreach (var item in keyValuePairs)
            {
                queryParameters.Add($@"@{item.Key}", item.Value);
            }

            return (sql, queryParameters);
        }
        public static (string sqlQuery, DynamicParameters queryParameters) GenerateUpdateSQLWithParameters<T>(T model, string identityColumn, string[] fieldsToSkip, string tableName) where T : class
        {
            Dictionary<string, dynamic> keyValuePairs =
                 JsonConvert.DeserializeObject<Dictionary<string, dynamic>>
                 (JsonConvert.SerializeObject(model));


            var identityValue = keyValuePairs[identityColumn];
            keyValuePairs = keyValuePairs.Where(r => r.Key != identityColumn && !fieldsToSkip.Contains(r.Key)).ToDictionary(r => r.Key, r => r.Value);

            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append($@"UPDATE {tableName} SET ");
            sqlBuilder.Append(string.Join(",", keyValuePairs.Select(item => $@" [{item.Key}] = @{item.Key}")));
            sqlBuilder.Append($@" Where [{identityColumn}] = @{identityColumn}");

            var sql = sqlBuilder.ToString();

            var queryParameters = new DynamicParameters();
            queryParameters.Add($@"@{identityColumn}", identityValue);
            foreach (var item in keyValuePairs)
            {
                queryParameters.Add($@"@{item.Key}", item.Value);
            }

            return (sql, queryParameters);
        }
    }
}
