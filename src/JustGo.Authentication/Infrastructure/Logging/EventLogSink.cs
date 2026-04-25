using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
    public class EventLogSink : ILogEventSink
    {
        private readonly IServiceProvider _serviceProvider;
        public EventLogSink(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var writeRepository = scope.ServiceProvider.GetService<IWriteRepository<SystemEvent>>();
                if (writeRepository == null) return;

                if (!logEvent.Properties.TryGetValue("DocId", out var docId))
                    return;

                string sql = "INSERT_SYSTEM_EVENT";
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@DocId", (int)((ScalarValue)logEvent.Properties["DocId"]).Value, dbType: DbType.Int32);
                queryParameters.Add("@ActionUserId", (int)((ScalarValue)logEvent.Properties["ActionUserId"]).Value, dbType: DbType.Int32);
                queryParameters.Add("@Source", "System");
                var category = ((ScalarValue)logEvent.Properties["Category"]).Value?.ToString();
                if (category.Contains("|"))
                {
                    var scheme = AuditScheme.GetCategorySubCategoryAction(category);
                    queryParameters.Add("@Category", scheme.Item1, dbType: DbType.Int32);
                    queryParameters.Add("@SubCategory", scheme.Item2, dbType: DbType.Int32);
                    queryParameters.Add("@Action", scheme.Item3, dbType: DbType.Int32);
                }
                else
                {
                    queryParameters.Add("@Category", (int)((ScalarValue)logEvent.Properties["Category"]).Value, dbType: DbType.Int32);
                    queryParameters.Add("@SubCategory", (int)((ScalarValue)logEvent.Properties["SubCategory"]).Value, dbType: DbType.Int32);
                    queryParameters.Add("@Action", (int)((ScalarValue)logEvent.Properties["Action"]).Value, dbType: DbType.Int32);
                }
                queryParameters.Add("@AffectedEntityType", (int)((ScalarValue)logEvent.Properties["AffectedEntityType"]).Value, dbType: DbType.Int32);
                string actionType = ((ScalarValue)logEvent.Properties["ActionType"]).Value?.ToString();
                if (actionType.Contains("|"))
                {
                    queryParameters.Add("@ActionType", GetActionType(actionType));
                }
                else
                {
                    queryParameters.Add("@ActionType", actionType);
                }
                queryParameters.Add("@OwningEntitydType", ((ScalarValue)logEvent.Properties["OwningEntitydType"]).Value);
                queryParameters.Add("@Details", ((ScalarValue)logEvent.Properties["Details"]).Value);
                queryParameters.Add("@ActionName", ((ScalarValue)logEvent.Properties["ActionName"]).Value);
                queryParameters.Add("@OwningEntityId", (int)((ScalarValue)logEvent.Properties["OwningEntityId"]).Value, dbType: DbType.Int32);
                writeRepository.Execute(sql, queryParameters);
            }
        }
        private string GetActionType(string aType)
        {
            string actionType = "";
            var action = aType.Split('|')[2];
            if (action.Contains("Create") || action.Contains("Add"))
            {
                actionType = "Created";
            }
            else if (action.Contains("Delete"))
            {
                actionType = "Deleted";
            }
            else if (action.Contains("Update"))
            {
                actionType = "Updated";
            }
            else if (action.Contains("Changed Status"))
            {
                actionType = "Changed Status";
            }
            else
            {
                actionType = "Updated";
            }
            return actionType;
        }

        public enum ActionType
        {
            [Description("Created")]
            Created = 1,

            [Description("Updated")]
            Updated = 2,

            [Description("Deleted")]
            Deleted = 3,

            [Description("Changed Status")]
            ChangedStatus = 4
        }
    }
}
