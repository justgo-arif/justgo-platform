using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace JustGo.Authentication.Infrastructure.Logging
{
    public static class CustomLog
    {
        public static void Event(
        string category,
        int actionUserId,
        int affectedEntityId,
        EntityType affectedEntityType,
        int owningEntityId,
        string message)
        {
            var actionName = message.Split(';').FirstOrDefault() ?? "";

            Log
                .ForContext("Category", category)
                .ForContext("ActionUserId", actionUserId)
                .ForContext("DocId", affectedEntityId)
                .ForContext("AffectedEntityType", (int)affectedEntityType)
                .ForContext("OwningEntityId", owningEntityId)
                .ForContext("Details", message)
                .ForContext("ActionType", category)
                .ForContext("OwningEntitydType", affectedEntityType.ToString())
                .ForContext("ActionName", actionName)
                .Information("Audit event: {ActionName}", actionName);
        }
        public static void Event(
        int category,
        int subCategory,
        int action,
        int actionUserId,
        int affectedEntityId,
        EntityType affectedEntityType,
        int owningEntityId,
        string actionType,
        string message)
        {
            var actionName = message.Split(';').FirstOrDefault() ?? "";

            Log
                .ForContext("Category", category)
                .ForContext("SubCategory", subCategory)
                .ForContext("Action", action)
                .ForContext("ActionUserId", actionUserId)
                .ForContext("DocId", affectedEntityId)
                .ForContext("AffectedEntityType", (int)affectedEntityType)
                .ForContext("OwningEntityId", owningEntityId)
                .ForContext("Details", message)
                .ForContext("ActionType", actionType)
                .ForContext("OwningEntitydType", affectedEntityType.ToString())
                .ForContext("ActionName", actionName)
                .Information("Audit event: {ActionName}", actionName);
        }

        public static void Exception(
        string traceId,
        string exceptionType,
        string exceptionMessage,
        string stackTrace,
        int statusCode,
        string errorCode,
        string errorMessage,
        int userId,
        string ipAddress,
        string userAgent)
        {
            Log
                .ForContext("TraceId", traceId)
                .ForContext("ExceptionType", exceptionType)
                .ForContext("ExceptionMessage", exceptionMessage)
                .ForContext("StackTrace", stackTrace)
                .ForContext("StatusCode", statusCode)
                .ForContext("ErrorCode", errorCode)
                .ForContext("ErrorMessage", errorMessage)
                .ForContext("UserId", userId)
                .ForContext("IPAddress", ipAddress)
                .ForContext("UserAgent", userAgent)
                .Error("Exception: {ExceptionType} [{StatusCode}] - {ErrorCode} - TraceId: {TraceId}",
                    exceptionType, statusCode, errorCode, traceId);
        }

    }
}
