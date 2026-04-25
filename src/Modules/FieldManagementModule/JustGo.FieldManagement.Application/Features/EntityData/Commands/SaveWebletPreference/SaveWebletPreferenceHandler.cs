using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.FieldManagement.Application.Features.EntityData.Commands.SaveWebletPreference;

public class SaveWebletPreferenceHandler : IRequestHandler<SaveWebletPreferenceCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepository;
    IUnitOfWork _unitOfWork;
    public SaveWebletPreferenceHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork)
    {
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(SaveWebletPreferenceCommand request, CancellationToken cancellationToken)
    {
        using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            string sql = @"
                IF EXISTS(Select Top 1 Isnull(wp.Value,'') PreferenceJsonValue
                From WebletPreference wp
                inner join [User] u on u.Userid = wp.UserId 
                where u.UserSyncId = @UserSyncId and wp.PreferenceType = @PreferenceType)
                BEGIN
	                UPDATE up set up.Value = @PreferenceJsonValue 
	                From [user] u 
	                inner join [WebletPreference] up on u.Userid = up.UserId 
	                where u.UserSyncId = @UserSyncId and up.PreferenceType = @PreferenceType
                END
                ELSE
                BEGIN
	                INSERT INTO [WebletPreference] (UserId, [Value], PreferenceType)
	                SELECT Top 1 u.Userid, Isnull(@PreferenceJsonValue,''), @PreferenceType
	                From [user] u 
	                Where u.UserSyncId = @UserSyncId
                END
                ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("UserSyncId", request.UserSyncId);
            queryParameters.Add("PreferenceType", request.PreferenceType);
            queryParameters.Add("PreferenceJsonValue", request.PreferenceJsonValue);

            int rowsAffected = await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
            await _unitOfWork.CommitAsync(dbTransaction);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(dbTransaction);
            throw ex.InnerException;
        }
    }

}
