using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Members.Commands.AddFamilyMember;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveUserPreference
{
    public class SaveUserPreferenceHandler : IRequestHandler<SaveUserPreferenceCommand, string>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUtilityService _utilityService;

        public SaveUserPreferenceHandler(IWriteRepositoryFactory writeRepositoryFactory, IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _utilityService = utilityService;
        }
        public string INSERT_SQL = """
                                   IF EXISTS (
                                       SELECT 1 
                                       FROM [dbo].[UserPreferences]
                                       WHERE UserID = @UserID 
                                         AND OrganizationID = @OrganizationID 
                                         AND IsActive = 1
                                   )
                                   BEGIN
                                       UPDATE [dbo].[UserPreferences]
                                       SET 
                                           PreferenceTypeID = @PreferenceTypeID,
                                           PreferenceValue = @PreferenceValue,
                                           UpdatedDate = GETUTCDATE()
                                       WHERE UserID = @UserID 
                                         AND OrganizationID = @OrganizationID 
                                         AND IsActive = 1;
                                   END
                                   ELSE
                                   BEGIN
                                       INSERT INTO [dbo].[UserPreferences] (
                                           [UserID],
                                           [OrganizationID],
                                           [PreferenceTypeID],
                                           [PreferenceValue],
                                           [CreatedDate],
                                           [UpdatedDate],
                                           [IsActive]
                                       )
                                       VALUES (
                                           @UserID,
                                           @OrganizationID,
                                           @PreferenceTypeID,
                                           @PreferenceValue,
                                           GETUTCDATE(),
                                           GETUTCDATE(),
                                           1
                                       );
                                   END
                                   """;

        public async Task<string> Handle(SaveUserPreferenceCommand request, CancellationToken cancellationToken)
        {
            var userId =
                await _utilityService.GetUserIdByMemberDocIdAsync(request.MemberDocId, cancellationToken);
            if (userId is null or 0)
            {
                return "User not found";
            }


            var parameters = new DynamicParameters();
            parameters.Add("@UserID", userId, System.Data.DbType.Int32);
            parameters.Add("@OrganizationID", request.OrganizationId, System.Data.DbType.Int32);
            parameters.Add("@PreferenceTypeID", request.PreferenceTypeId, System.Data.DbType.Int32);
            parameters.Add("@PreferenceValue", request.PreferenceValue, System.Data.DbType.String);

            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            await repo.ExecuteAsync(
               INSERT_SQL,
               cancellationToken,
               parameters,
               dbTransaction: null,
               commandType: "text"
           );


            return "Success";
        }
    }
}
