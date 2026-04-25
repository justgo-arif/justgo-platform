using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Commands.DeleteTenant
{
    public class DeleteTenantCommand : IRequest<int>
    {
        public DeleteTenantCommand(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
    }
}
