using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.CustomMediator
{
    /// <summary>
    /// Pipeline behavior for handling cross-cutting concerns
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : class
    {
        /// <summary>
        /// Handle the request with pipeline behavior
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="next">Next delegate in pipeline</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response</returns>
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Delegate for the next handler in the pipeline
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <returns>Response</returns>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
}
