using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    class CoachListQueryHandler : IRequestHandler<CoachListQuery, IEnumerable<object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;

        public CoachListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
           
        }
        public async Task<IEnumerable<object>> Handle(CoachListQuery request, CancellationToken cancellationToken)
        {

            string sql = @"SELECT c.MemberDocId,
                MAX(BookingContactId) AS BookingContactId,MAX(u.UserId) AS UserId,
                MAX(u.Gender) AS Gender,MAX(u.ProfilePicURL) AS ProfilePicURL,
                MAX(CONCAT(c.FirstName, ' ', c.LastName)) AS CoachName,MAX(cs.SessionId) as SessionId
            FROM JustGoBookingContact c
            INNER JOIN JustGoBookingClassSession cs 
                ON c.EntityId = cs.SessionId
            INNER JOIN JustGoBookingClass cl 
                ON cs.ClassId = cl.ClassId
            LEFT JOIN [User] u 
                ON c.MemberDocId = u.MemberDocId
            WHERE cl.OwningEntityId = @CloubDocId 
              AND cl.IsDeleted <> 1
            GROUP BY c.MemberDocId;";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CloubDocId", request.ClubDocId);

            return  await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

           
        }
    }
}
