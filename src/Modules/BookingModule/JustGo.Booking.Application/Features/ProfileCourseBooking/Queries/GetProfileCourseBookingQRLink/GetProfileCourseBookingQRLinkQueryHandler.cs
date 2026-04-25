using System.Data;
using System.Data.SqlTypes;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.ProfileBookingDtos;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.ProfileCourseBooking.Queries.GetProfileCourseBookingQRLink
{
    public class GetProfileCourseBookingQRLinkQueryHandler : IRequestHandler<GetProfileCourseBookingQRLinkQuery, EventQRLink>
    {
        private readonly LazyService<IReadRepository<string>> _readRepository;

        public GetProfileCourseBookingQRLinkQueryHandler(LazyService<IReadRepository<string>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<EventQRLink> Handle(GetProfileCourseBookingQRLinkQuery request, CancellationToken cancellationToken)
        {
           string sql = GetEventWalletQrUrl();




            var parameters = new DynamicParameters();
            parameters.Add("@BookingSyncId", request.BookingGuid, DbType.Guid);

            var result = await _readRepository.Value.GetSingleAsync<string>(sql, parameters, null, cancellationToken, "text");
            return new EventQRLink {QRLink=result??""};
        }

        private Func<string> GetEventWalletQrUrl = () => @"
            SELECT 
                CONCAT(
                    (SELECT [Value] 
                     FROM SystemSettings 
                     WHERE ItemKey = 'SYSTEM.AZURESTOREROOT'),
                    '/002/',
                    ss.Value,
                    '/',
                    REPLACE(uq.QrImageUrl, '\', '/')
                ) AS QRLink
            FROM SystemSettings ss
            INNER JOIN EventQueue eq ON 1 = 1
            INNER JOIN Document d ON d.DocId = eq.EntityDocId
            LEFT JOIN UserQrCodes uq ON uq.EventQueueId = eq.EventId
            WHERE 
                ss.ItemKey = 'CLUBPLUS.HOSTSYSTEMID'
                AND d.SyncGuid = @BookingSyncId;";
    }
}
