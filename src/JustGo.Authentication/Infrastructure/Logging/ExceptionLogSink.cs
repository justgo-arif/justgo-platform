using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace JustGo.Authentication.Infrastructure.Logging
{
    public class ExceptionLogSink : ILogEventSink
    {
        private readonly IServiceProvider _serviceProvider;

        public ExceptionLogSink(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var writeRepository = scope.ServiceProvider.GetService<IWriteRepository<ExceptionLog>>();
                if (writeRepository == null) return;

            if (!logEvent.Properties.TryGetValue("TraceId", out var traceId))
                return;

                string sql = @"INSERT INTO [dbo].[ExceptionLogs]
                                   ([TraceId]
                                   ,[Timestamp]
                                   ,[ExceptionType]
                                   ,[ExceptionMessage]
                                   ,[StackTrace]
                                   ,[StatusCode]
                                   ,[ErrorCode]
                                   ,[ErrorMessage]
                                   ,[UserId]
                                   ,[IPAddress]
                                   ,[UserAgent])
                             VALUES
                                   (@TraceId
                                   ,@Timestamp
                                   ,@ExceptionType
                                   ,@ExceptionMessage
                                   ,@StackTrace
                                   ,@StatusCode
                                   ,@ErrorCode
                                   ,@ErrorMessage
                                   ,@UserId
                                   ,@IPAddress
                                   ,@UserAgent)";
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@TraceId", ((ScalarValue)logEvent.Properties["TraceId"]).Value);
                queryParameters.Add("@Timestamp", DateTime.UtcNow, dbType: DbType.DateTime2);
                queryParameters.Add("@ExceptionType", ((ScalarValue)logEvent.Properties["ExceptionType"]).Value);
                queryParameters.Add("@ExceptionMessage", ((ScalarValue)logEvent.Properties["ExceptionMessage"]).Value);
                queryParameters.Add("@StackTrace", ((ScalarValue)logEvent.Properties["StackTrace"]).Value);
                queryParameters.Add("@StatusCode", (int)((ScalarValue)logEvent.Properties["StatusCode"]).Value, dbType: DbType.Int32);
                queryParameters.Add("@ErrorCode", ((ScalarValue)logEvent.Properties["ErrorCode"]).Value);
                queryParameters.Add("@ErrorMessage", ((ScalarValue)logEvent.Properties["ErrorMessage"]).Value);
                queryParameters.Add("@UserId", (int)((ScalarValue)logEvent.Properties["UserId"]).Value, dbType: DbType.Int32);
                queryParameters.Add("@IPAddress", ((ScalarValue)logEvent.Properties["IPAddress"]).Value);
                queryParameters.Add("@UserAgent", ((ScalarValue)logEvent.Properties["UserAgent"]).Value);
                writeRepository.Execute(sql, queryParameters, null, "text");
            }
        }
    }
}
