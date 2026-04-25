using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberSummaryBySyncGuid;
using JustGo.MemberProfile.Domain.Entities;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.UpdateMember;

public class UpdateMemberHandler : IRequestHandler<UpdateMemberCommand, OperationResultDto<MemberSummaryDto>>
{
    private readonly IWriteRepositoryFactory _writeRepository;
    IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    public UpdateMemberHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<OperationResultDto<MemberSummaryDto>> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var msg = await UpdateBasicDetails(request, cancellationToken);

        if (msg == "Success")
        {
            var member = await _mediator.Send(new GetMemberSummaryBySyncGuidQuery(request.UserSyncId), cancellationToken);
            return new OperationResultDto<MemberSummaryDto>
            {
                IsSuccess = true,
                Message = "Member updated successfully",
                RowsAffected = 1,
                Data = member
            };
        }
        else
        {
            return new OperationResultDto<MemberSummaryDto>
            {
                IsSuccess = false,
                Message = msg,
                RowsAffected = 0
            };
        }
    }

    private async Task<string> UpdateBasicDetails(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            string sql = @"
                    BEGIN TRY
                        BEGIN TRANSACTION

	                    IF EXISTS (SELECT TOP 1 1 FROM [User] WHERE LoginId = @LoginId AND UserSyncId != @UserSyncId)
	                    BEGIN
		                    ROLLBACK TRANSACTION;
		                    THROW 50000, 'This username is already exist!', 1;
		                    RETURN;
	                    END

                        IF (ISNULL(@Country, '') <> '' AND @CountryId = 0)
                        BEGIN
                            DECLARE @NewCountryId INT;
                            DECLARE @Sql NVARCHAR(MAX) = (SELECT dbo.GetLookupTableQuery('Country'));

                            SET @Sql = '
                                WITH LU AS (' + @Sql + ')
                                SELECT TOP 1 @InnerId = RowId
                                FROM LU WHERE Country = ''' + REPLACE(@Country, '''', '''''') + '''';

                            EXEC sp_executesql 
                                @stmt = @Sql, 
                                @params = N'@InnerId INT OUTPUT',
                                @InnerId = @NewCountryId OUTPUT;

                            IF(ISNULL(@NewCountryId, 0) <= 0)
                            BEGIN
                                ROLLBACK TRANSACTION;
		                        THROW 50000, 'Please select a valid country.', 1;
		                        RETURN;
                            END

                            SET @CountryId = @NewCountryId;
                        END

                        DECLARE @PreviousEmail VARCHAR(100) = (SELECT TOP 1 [EmailAddress] FROM [User] WHERE UserSyncId = @UserSyncId);

	                   UPDATE [User] SET
                           LoginId = @LoginId
                           ,[FirstName] = @FirstName
                           ,[LastName] = @LastName
                           ,[LastUpdateDate] = GETDATE()
                           ,[LastEditDate] = GETUTCDATE()
                           --,Mobile = IIF(@CountryCode IS NOT NULL, CONCAT(@CountryCode, ' ', @Mobile), @Mobile)
                           ,Mobile = @Mobile
                           ,[EmailAddress] = @EmailAddress
                           ,[DOB] = @DOB
                           ,[Gender] = @Gender
                           ,[Address1] = @Address1
                           ,[Address2] = @Address2
                           ,[Address3] = @Address3
                           ,[Town] = @Town
                           ,[County] = @County
                           ,[Country] = @Country
                           ,[PostCode] = @PostCode      
                           ,[CountryId] = @CountryId
                           ,[CountyId] = @CountyId
                       WHERE UserSyncId = @UserSyncId;

                        DECLARE @MemberDocId INT = (SELECT TOP 1 MemberDocId FROM [User] WHERE UserSyncId = @UserSyncId);
                        UPDATE Members_Default SET Tempfirstname = @FirstName, Tempsurname = @LastName
                        WHERE DocID = @MemberDocId;

	                    UPDATE UPN SET UPN.Number = @Mobile, UPN.CountryCode = @CountryCode
	                    FROM UserPhoneNumber UPN
	                    WHERE UPN.Id = (
		                    SELECT TOP 1 P.[Id]
		                    FROM UserPhoneNumber P
		                    INNER JOIN [User] U ON U.Userid = P.UserId
		                    AND P.[Type] = 'Mobile'
		                    WHERE U.UserSyncId = @UserSyncId
		                    ORDER BY P.[Id] ASC
	                    );

                        DECLARE @CurrentEmail VARCHAR(100) = (SELECT TOP 1 [EmailAddress] FROM [User] WHERE UserSyncId = @UserSyncId);
                        IF(@PreviousEmail != @CurrentEmail)
                        BEGIN
                            UPDATE [User] SET EmailVerified = NULL WHERE UserSyncId = @UserSyncId;
                        END

                        COMMIT TRANSACTION
                    END TRY
                    BEGIN CATCH
                        IF (XACT_STATE() <> 0)
                            ROLLBACK TRANSACTION;

                        -- Get error info
                        DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
                        SELECT 
                            @ErrorMessage = ERROR_MESSAGE(),
                            @ErrorSeverity = ERROR_SEVERITY(),
                            @ErrorState = ERROR_STATE();

                        -- Re-throw the error with details
                        RAISERROR('%s', @ErrorSeverity, @ErrorState, @ErrorMessage);
                        RETURN;
                    END CATCH
                    ";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", request.LoginId);
            queryParameters.Add("@FirstName", request.FirstName);
            queryParameters.Add("@LastName", request.LastName);
            queryParameters.Add("@Mobile", request.Mobile);
            queryParameters.Add("@CountryCode", request.CountryCode);
            queryParameters.Add("@EmailAddress", request.EmailAddress);
            queryParameters.Add("@DOB", request.DOB, dbType: DbType.Date);
            queryParameters.Add("@Gender", request.Gender);
            queryParameters.Add("@Address1", request.Address1);
            queryParameters.Add("@Address2", request.Address2);
            queryParameters.Add("@Address3", request.Address3);
            queryParameters.Add("@Town", request.Town);
            queryParameters.Add("@County", request.County);
            queryParameters.Add("@Country", request.Country);
            queryParameters.Add("@PostCode", request.PostCode);
            queryParameters.Add("@CountryId", request.CountryId, dbType: DbType.Int32);
            queryParameters.Add("@CountyId", request.CountyId, dbType: DbType.Int32);
            queryParameters.Add("@UserSyncId", request.UserSyncId, dbType: DbType.Guid);
            int rowsAffected = await _writeRepository.GetLazyRepository<MemberSummary>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
            await _unitOfWork.CommitAsync(dbTransaction);
            
            if (rowsAffected > 0) return "Success";
            else return "No rows affected";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(dbTransaction);
            return ex.Message;
        }
        
    }
}
