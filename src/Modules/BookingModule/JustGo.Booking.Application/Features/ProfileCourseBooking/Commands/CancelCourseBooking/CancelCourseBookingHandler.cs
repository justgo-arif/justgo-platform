using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using System.Data;

namespace JustGo.Booking.Application.Features.ProfileCourseBooking.Commands.CancelCourseBooking;


public class CancelCourseBookingHandler:IRequestHandler<CancelCourseBookingCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public CancelCourseBookingHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<int> Handle(CancelCourseBookingCommand request, CancellationToken cancellationToken)
    {

        var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        int userId = await _utilityService.GetCurrentUserId(cancellationToken);

        const string query =
            """
            DECLARE @LogDate DATETIME = GETUTCDATE();

            DECLARE @CancelledStateId INT = (SELECT TOP 1 s.StateId FROM Process p
                INNER JOIN Repository r ON r.RepositoryId = p.PrimaryRepositoryId
                INNER JOIN [State] s ON s.ProcessId = p.ProcessId
                WHERE r.[Name] = 'Course Booking' AND s.[Name] = 'Cancelled'
            );
            DECLARE @CourseBookingDocId INT = (SELECT TOP 1 d.DocId FROM CourseBooking_Default td
                INNER JOIN Document d ON d.DocId = td.DocId
                WHERE d.SyncGuid = @CourseSyncId
            );

            UPDATE ProcessInfo SET PreviousStateId = CurrentStateId,CurrentStateId = @CancelledStateId,LastActionId = 0
            ,LastActionDate = @LogDate,Info = 'Successfully Executed /Cancel'
            WHERE PrimaryDocId = @CourseBookingDocId;

            INSERT INTO ProcessLog(InstanceId, CurrentStateId, PreviousStateId, ActionId, ActionUser, LogDate, Info)
            SELECT InstanceId, CurrentStateId, PreviousStateId, LastActionId, @ActionUserId, @LogDate, Info FROM ProcessInfo
            WHERE PrimaryDocId = @CourseBookingDocId;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@CourseSyncId", request.Id,DbType.Guid);
        parameters.Add("@ActionUserId", userId, DbType.Int32);


        var affectedRows = await repo.ExecuteAsync(query, cancellationToken, parameters, transaction, "text");

        await _unitOfWork.CommitAsync(transaction);

        return affectedRows;

    }
}
