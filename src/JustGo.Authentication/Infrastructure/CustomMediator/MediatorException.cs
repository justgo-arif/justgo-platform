using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.CustomMediator
{
    /// <summary>
    /// Base exception for mediator-related errors
    /// </summary>
    public class MediatorException : Exception
    {
        public MediatorException(string message) : base(message)
        {
        }
        public MediatorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    /// <summary>
    /// Exception thrown when a handler is not found
    /// </summary>
    public class HandlerNotFoundException : MediatorException
    {
        public Type RequestType { get; }

        public HandlerNotFoundException(Type requestType)
            : base($"No handler found for request type {requestType.Name}")
        {
            RequestType = requestType;
        }
    }

    /// <summary>
    /// Exception thrown when multiple handlers are found for a single request
    /// </summary>
    public class MultipleHandlersException : MediatorException
    {
        public Type RequestType { get; }

        public MultipleHandlersException(Type requestType)
            : base($"Multiple handlers found for request type {requestType.Name}")
        {
            RequestType = requestType;
        }
    }
}
