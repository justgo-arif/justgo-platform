using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UserInvitation;

public class UserInvitationHandler : IRequestHandler<UserInvitationCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public UserInvitationHandler(
        IWriteRepositoryFactory writeRepositoryFactory,
        IUnitOfWork unitOfWork,
        IUtilityService utilityService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<int> Handle(UserInvitationCommand request, CancellationToken cancellationToken = default)
    {
        var currentUser = await _utilityService.GetCurrentUserPublic(cancellationToken);
        var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
        using var transaction = await _unitOfWork.BeginTransactionAsync();

        var sql = """
            IF EXISTS (SELECT TOP 1 * FROM JustGoBookingWaitlistHistory WHERE HistoryGuid = @InviteId AND IsUserSynced = 0)
            BEGIN
                DECLARE @WaitListId INT = (SELECT TOP 1 WaitListId FROM JustGoBookingWaitlistHistory WHERE HistoryGuid = @InviteId);
                UPDATE JustGoBookingWaitlist SET EntityDocId = @EntityDocId WHERE WaitListId = @WaitListId;
                UPDATE JustGoBookingWaitlistHistory SET IsUserSynced = 1 WHERE HistoryGuid = @InviteId;
            END
            """;

        int affectedRows = await repo.ExecuteAsync(sql, cancellationToken, new { InviteId = request.InviteId, EntityDocId = currentUser?.MemberDocId }, transaction, "Text");
        await _unitOfWork.CommitAsync(transaction);
        return affectedRows;
    }
}
