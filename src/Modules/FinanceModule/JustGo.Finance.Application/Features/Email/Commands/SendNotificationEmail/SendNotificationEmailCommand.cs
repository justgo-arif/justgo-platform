using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Email.Commands.SendNotificationEmail
{
    public record SendNotificationEmailCommand(
    string Sender,
    string Recipient, 
    string EmailId,
    int OwningEntityId
    ) : IRequest<bool>;
}
