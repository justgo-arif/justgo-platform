using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Configuration;
using Serilog;

namespace JustGo.Authentication.Infrastructure.Logging
{
    public static class AuditLogSinkExtensions
    {
        public static LoggerConfiguration AuditSink(this LoggerSinkConfiguration loggerSinkConfiguration,
        IServiceProvider serviceProvider)
        {
            return loggerSinkConfiguration.Sink(new AuditLogSink(serviceProvider));
        }
        public static LoggerConfiguration EventSink(this LoggerSinkConfiguration loggerSinkConfiguration,
        IServiceProvider serviceProvider)
        {
            return loggerSinkConfiguration.Sink(new EventLogSink(serviceProvider));
        }
        public static LoggerConfiguration ExceptionSink(this LoggerSinkConfiguration loggerSinkConfiguration,
        IServiceProvider serviceProvider)
        {
            return loggerSinkConfiguration.Sink(new ExceptionLogSink(serviceProvider));
        }
    }
}
