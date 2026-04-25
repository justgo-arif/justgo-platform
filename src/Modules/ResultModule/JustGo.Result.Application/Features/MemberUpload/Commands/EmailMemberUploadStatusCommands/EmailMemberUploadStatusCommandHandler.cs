using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.EmailMemberUploadStatusCommands;

public class EmailMemberUploadStatusCommandHandler : IRequestHandler<EmailMemberUploadStatusCommand, Result<string>>
{
    
    private readonly IWriteRepositoryFactory _writeRepoFactory;

    public EmailMemberUploadStatusCommandHandler(IWriteRepositoryFactory writeRepoFactory)
    {
        _writeRepoFactory = writeRepoFactory;
    }

    public async Task<Result<string>> Handle(EmailMemberUploadStatusCommand request, CancellationToken cancellationToken)
    {
        const string updateSql = """
                                  IF NOT EXISTS (
                                      SELECT 1 
                                      FROM ResultUploadingFileNotificationFlag 
                                      WHERE FileId = @UploadedFileId
                                    )
                                   BEGIN
                                 	INSERT INTO ResultUploadingFileNotificationFlag (FileId, Flag)
                                 	VALUES(@UploadedFileId, 1)
                                   END;
                                   
                                   SELECT TOP 1 U.EmailAddress 
                                     FROM ResultUploadedFile F
                                     INNER JOIN [User] U on F.UpdatedBy = U.Userid
                                     WHERE UploadedFileId = @UploadedFileId
                                 """;
        
        var parameters = new { UploadedFileId = request.FileId };
        var writeRepo = _writeRepoFactory.GetRepository<EmailMemberUploadStatusCommand>();

        var emailAddress = await writeRepo.ExecuteScalarAsync<string>(updateSql, cancellationToken, parameters, null, QueryType.Text);
        
        return emailAddress ?? Result<string>.Failure("No email address found for the uploaded file.", ErrorType.BadRequest);
    }  
}