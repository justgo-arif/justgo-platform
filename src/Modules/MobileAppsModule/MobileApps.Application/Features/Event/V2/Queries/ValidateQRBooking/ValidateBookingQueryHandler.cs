using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.ValidateBooking
{
    class ValidateBookingQueryHandler : IRequestHandler<ValidateBookingQuery,Dictionary<string, object>>
    {
        private readonly LazyService<IReadRepository<object>> _readObjRepository;
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private IMediator _mediator;

        public ValidateBookingQueryHandler(LazyService<IReadRepository<object>> readObjRepository, LazyService<IReadRepository<string>> readRepository, LazyService<IWriteRepository<object>> writeRepository, IMediator mediator)
        {
            _readObjRepository = readObjRepository;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _mediator = mediator;
        }

        public async Task<Dictionary<string, object>> Handle(ValidateBookingQuery request, CancellationToken cancellationToken)
        {
            var resultData = new Dictionary<string, object>();

                string sql = @"select DocId  from Document  where SyncGuid=@CourseBookingSyncGuid AND RepositoryId=6;";
           var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingSyncGuid", request.DocumentSyncGuid);

            var resultDocument = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");


               string sqlMember = @"select DocId from Document where SyncGuid=@MemberSyncGuid and repositoryid = 1;";
            var queryParametersMember = new DynamicParameters();
            queryParametersMember.Add("@MemberSyncGuid", request.MemberSyncGuid);
            var resultMember = await _readRepository.Value.GetAsync(sqlMember, queryParametersMember, null, "text");

                


            if (!string.IsNullOrEmpty(resultDocument) && !string.IsNullOrEmpty(resultMember))
            {

                    string sqlCourse = $@"select cbd.DocId,ISNULL(ed.Isrecurring,0) as IsRecurring,ed.DocId as EventDocId  from CourseBooking_Default  cbd
                                   INNER JOIN Events_Default as ed ON ed.DocId = cbd.Coursedocid
                                    where cbd.DocId=@DocumentDocId";

                var queryParametersCourse = new DynamicParameters();
                queryParametersCourse.Add("@DocumentDocId", Convert.ToInt32(resultDocument));

                var result = await _readObjRepository.Value.GetAsync(sqlCourse, queryParametersCourse, null, "text");

                resultData = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(result));

            }
            return resultData;
        }
        
    }
}
