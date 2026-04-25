using Dapper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.Booking.Application.Features.ClassTerm.Commands.RemoveTermHoliday
{
    public class RemoveJustGoBookingTermHolidayHandler : IRequestHandler<RemoveJustGoBookingTermHolidayCommand, string>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        private const string DeleteSql = """
                                         DECLARE @TermId INT, @HolidayName NVARCHAR(100);

                                         SELECT @TermId = h.TermId, @HolidayName = h.HolidayName
                                         FROM JustGoBookingClassTermHoliday h
                                         WHERE h.TermHolidayId = @TermHolidayId
                                           AND h.IsDeleted = 0;

                                         IF @TermId IS NULL
                                         BEGIN
                                             RETURN;
                                         END

                                         -- Capture dates before deletion
                                         DECLARE @DateList TABLE (HolidayDate DATE PRIMARY KEY);
                                         INSERT INTO @DateList (HolidayDate)
                                         SELECT HolidayDate
                                         FROM JustGoBookingClassTermHolidayDates
                                         WHERE TermHolidayId = @TermHolidayId;
                                         
                                         DELETE ah
                                         FROM JustGoBookingAdditionalHoliday ah
                                         INNER JOIN JustGoBookingClassSession cs
                                             ON ah.SessionId = cs.SessionId AND cs.IsDeleted = 0
                                         INNER JOIN JustGoBookingClassSessionSchedule css
                                             ON cs.SessionId = css.SessionId AND css.IsDeleted = 0
                                         INNER JOIN JustGoBookingScheduleOccurrence so
                                             ON css.SessionScheduleId = so.ScheduleId AND so.IsDeleted = 0
                                         INNER JOIN @DateList dl
                                             ON dl.HolidayDate BETWEEN CAST(so.StartDate AS DATE) AND CAST(so.EndDate AS DATE)
                                         WHERE cs.TermId = @TermId
                                           AND ah.IsDeleted = 0
                                           AND ah.Name = @HolidayName;

                                         -- Delete holiday date rows
                                         DELETE FROM JustGoBookingClassTermHolidayDates
                                         WHERE TermHolidayId = @TermHolidayId;

                                         -- Soft delete holiday master
                                         UPDATE JustGoBookingClassTermHoliday
                                         SET IsDeleted = 1
                                         WHERE TermHolidayId = @TermHolidayId
                                           AND IsDeleted = 0;
                                         """;
        public RemoveJustGoBookingTermHolidayHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<string> Handle(RemoveJustGoBookingTermHolidayCommand request, CancellationToken cancellationToken = default)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);


            var parameters = new DynamicParameters();
            parameters.Add("@TermHolidayId", request.TermHolidayId, DbType.Int32);

            await repo.ExecuteAsync(DeleteSql, cancellationToken, parameters, transaction, "text");

            CustomLog.Event(
                        "Class Management|Term Holiday|Deleted",
                        currentUserId,
                        request.TermHolidayId,
                        EntityType.ClassManagement,
                        -1,
                        "Term Holiday Deleted;"
                    );

            await _unitOfWork.CommitAsync(transaction);

            return "Deleted Successfully";
        }
    }
}
