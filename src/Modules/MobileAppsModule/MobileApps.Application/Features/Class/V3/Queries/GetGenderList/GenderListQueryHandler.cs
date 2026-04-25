using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    class GenderListQueryHandler : IRequestHandler<GenderListQuery, string[]>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;

        public GenderListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
           
        }
        public async Task<string[]> Handle(GenderListQuery request, CancellationToken cancellationToken)
        {

            return new[] { "Male", "Female", "Prefer not to say" };

        }
    }
}
