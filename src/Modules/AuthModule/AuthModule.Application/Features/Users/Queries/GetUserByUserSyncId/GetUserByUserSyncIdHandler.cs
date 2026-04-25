using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId
{
    public class GetUserByUserSyncIdHandler : IRequestHandler<GetUserByUserSyncIdQuery, User>
    {
        private readonly LazyService<IReadRepository<User>> _readRepository;

        public GetUserByUserSyncIdHandler(LazyService<IReadRepository<User>> readRepository)
        {
            this._readRepository = readRepository;
        }

        public async Task<User> Handle(GetUserByUserSyncIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT [Userid]
                              ,[LoginId]
                              ,[Title]
                              ,[FirstName]
                              ,[MiddleName]
                              ,[LastName]
                              ,[Password]
                              ,[Mobile]
                              ,[Fax]
                              ,[CreationDate]
                              ,[LastUpdateDate]
                              ,[LastLoginDate]
                              ,[LastPasswordUpdateDate]
                              ,[FailedLoginAttempt]
                              ,[IsActive]
                              ,[IsLocked]
                              ,[Comments]
                              ,[LastEditDate]
                              ,[EmailAddress]
                              ,[ForceResetPassword]
                              ,[ProfilePicURL]
                              ,[DOB]
                              ,[Gender]
                              ,[Address1]
                              ,[Address2]
                              ,[Address3]
                              ,[Town]
                              ,[County]
                              ,[Country]
                              ,[PostCode]
                              ,[Currency]
                              ,[MemberDocId]
                              ,[ParentFirstname]
                              ,[ParentLastname]
                              ,[ParentEmailAddress]
                              ,[ParentEmailVerified]
                              ,[EmailVerified]
                              ,[ParentalOverrideUser]
                              ,[ParentalOverrideDate]
                              ,[SourceUserId]
                              ,[SourceLocation]
                              ,[OtherGender]
                              ,[MemberId]
                              ,[CountryId]
                              ,[CountyId]
                              ,[UserSyncId]
                              ,[SuspensionLevel]
                          FROM [dbo].[User]
                          WHERE [UserSyncId]=@UserSyncId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", request.UserSyncId, dbType: DbType.Guid);
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
