using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    class ClassColorGroupListQueryHandler : IRequestHandler<ClassColorGroupListQuery, IEnumerable<object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;

        public ClassColorGroupListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
           
        }
        public async Task<IEnumerable<object>> Handle(ClassColorGroupListQuery request, CancellationToken cancellationToken)
        {

            string sql = @"select * from JustGoBookingClassColorGroup";

            return await _readRepository.Value.GetListAsync(sql, null, null, "text");
            
        }
    }
}
