using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.CustomMediator
{
    /// <summary>
    /// Mediator interface for handling requests
    /// </summary>
    public interface IMediator
    {
        /// <summary>
        /// Send a request with return value
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="request">Request instance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response</returns>
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
