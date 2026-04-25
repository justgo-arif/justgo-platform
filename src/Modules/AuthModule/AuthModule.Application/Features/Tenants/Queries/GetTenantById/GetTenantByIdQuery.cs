using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantById
{
    public class GetTenantByIdQuery : IRequest<Tenant>
    {
        public int Id { get; set; }
        public GetTenantByIdQuery(int id)
        {
            Id = id;
        }

    }
}
