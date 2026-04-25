using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.CustomAuthorizations
{
    public interface IAuthorizationService
    {
        Task IsActionAllowedAsync(int invokingUserId, int docId, string option, CancellationToken cancellationToken);
    }
}
