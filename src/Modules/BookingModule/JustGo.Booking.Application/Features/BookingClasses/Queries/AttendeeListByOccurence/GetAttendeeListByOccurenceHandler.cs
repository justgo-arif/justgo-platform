using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using System.Data;


namespace JustGo.Booking.Application.Features.BookingClasses.Queries.AttendeeListByOccurence
{
    public class GetAttendeeListByOccurenceHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<GetAttendeeListByOccurenceQuery, SessionAttendanceResponseDto>
    {
        private readonly IReadRepositoryFactory _readRepository = readRepository;

        public async Task<SessionAttendanceResponseDto> Handle(GetAttendeeListByOccurenceQuery request,
            CancellationToken cancellationToken)
        {
            var repo = _readRepository.GetLazyRepository<SessionAttendanceResponseDto>().Value;

            var parameters = new DynamicParameters();
            parameters.Add("@SessionGuid", request.SessionGuid.ToString(), DbType.String, size: 100);
            parameters.Add("@OwnerGuid", request.OwnerGuid.ToString(), DbType.String, size: 100);
            parameters.Add("@OccurrenceId", request.OccurrenceId, DbType.Int32);
            parameters.Add("@RowsPerPage", request.RowsPerPage, DbType.Int32);
            parameters.Add("@PageNumber", request.PageNumber, DbType.Int32);
            parameters.Add("@IsActiveMemberOnly", request.IsActiveMemberOnly, DbType.Boolean);
            parameters.Add("@FilterValue", request.FilterValue ?? string.Empty, DbType.String);


            await using var multiResult = await repo.GetMultipleQueryAsync(
                "ClassAttendeeListByOccurence",
                cancellationToken,
                parameters);

            var response = new SessionAttendanceResponseDto
            {
                SessionInfo = await multiResult.ReadSingleOrDefaultAsync<SessionInfoDto>() ?? new SessionInfoDto(),
                Statistics = await multiResult.ReadSingleOrDefaultAsync<SessionStatisticsDto>() ?? new SessionStatisticsDto()
            };
            var attendees = (await multiResult.ReadAsync<AttendeeDto>()).ToList();
            var totalCount = await multiResult.ReadSingleOrDefaultAsync<int>();
            response.AttendeeList.Attendees = attendees;
            response.AttendeeList.TotalCount= totalCount;
            response.AttendeeList.PageNumber = request.PageNumber;
            response.AttendeeList.PageSize = request.RowsPerPage;
            return response;

        }
    }
}
