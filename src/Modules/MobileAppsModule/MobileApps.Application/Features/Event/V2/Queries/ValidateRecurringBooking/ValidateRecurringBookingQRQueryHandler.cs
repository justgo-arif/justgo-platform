using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList;
using MobileApps.Domain.Entities.V2.Event;

namespace MobileApps.Application.Features.Event.V2.Queries.ValidateRecurringBooking
{
    class ValidateRecurringBookingQRQueryHandler : IRequestHandler<ValidateRecurringBookingQRQuery, Tuple<IDictionary<string, object>, bool>>
    {
        private readonly LazyService<IReadRepository<object>> _readObjRepository;
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private IMediator _mediator;

        public ValidateRecurringBookingQRQueryHandler(LazyService<IReadRepository<object>> readObjRepository, LazyService<IReadRepository<string>> readRepository, LazyService<IWriteRepository<object>> writeRepository, IMediator mediator)
        {
            _readObjRepository = readObjRepository;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _mediator = mediator;
        }

        public async Task<Tuple<IDictionary<string, object>, bool>> Handle(ValidateRecurringBookingQRQuery request, CancellationToken cancellationToken)
        {
            IList<BookingDate> resultData = new List<BookingDate>();
            IDictionary<string, object> finalModel = new Dictionary<string, object>();


            if (request.DocId > 0)
            {


                //update booking status

                if (await IsNotChecking(Convert.ToInt32(request.DocId)))
                {
                    await UpdateBookingCheckedInStatus(request.DocId);

                }
                finalModel.Add("IsBookingValid", true);
                resultData = await GetOccuranceBookingDateList(Convert.ToInt32(request.DocId));
                finalModel.Add("BookingDateList", resultData);
            }
            else
            {
                finalModel.Add("IsBookingValid", false);
                finalModel.Add("BookingDateList", resultData);
            }

            return Tuple.Create(finalModel, true);
        }



        private async Task UpdateBookingCheckedInStatus(int CourseBookingDocId)
        {
            string sql = @"UPDATE CourseBooking_Default
                        SET Checkedin = 1
                        Where DocId = @CourseBookingDocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", CourseBookingDocId);

            await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");
        }

        private async Task<bool> IsNotChecking(int CourseBookingDocId)
        {
            //if data null or empty then return not check in true 
            string sql = @"select DocId  from CourseBooking_Default  where DocId=@CourseBookingDocId AND Checkedin=1";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", CourseBookingDocId);

            var data =await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");

            return string.IsNullOrEmpty(data);
        }

        private async Task<IList<BookingDate>> GetOccuranceBookingDateList(int DDocId)
        {

            string sql = @"select EventRecurringScheduleTicket.EventRecurringScheduleIntervalRowId
                            from Document cdoc
                            inner join CourseBooking_Default cbd on cbd.DocId = cdoc.DocId
                            inner join EventRecurringScheduleTicket on EventRecurringScheduleTicket.TicketDocId = cbd.Productdocid
                            where  cbd.DocId=@DocumentDocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@DocumentDocId", DDocId);
            var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
            
            return !string.IsNullOrEmpty(result)? await _mediator.Send(new GetRecurringOccuranceBookingDateListQuery { RowId= Convert.ToInt32(result) }):null;

        }


    }
}
