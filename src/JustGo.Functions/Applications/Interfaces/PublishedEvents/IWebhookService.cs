using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Functions.Domains.Models;
using JustGo.Functions.Domains.Models.PublishedEvents;

namespace JustGo.Functions.Applications.Interfaces.PublishedEvents
{
    public interface IWebhookService
    {
        Task<List<PublishedEventResponses>> SendWebhookAsync(
        EventMessage request,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);
    }
}
