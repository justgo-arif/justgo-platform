using AuthModule.Application.Features.Files.Commands.FileUpload;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberBasicInfoBySyncGuid;
using JustGo.MemberProfile.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.SetMemberPhoto;

public class SetMemberPhotoCommandHandler : IRequestHandler<SetMemberPhotoCommand, OperationResultDto<string>>
{
    private readonly IAzureBlobFileService _fileSystemService;
    private readonly LazyService<IWriteRepository<object>> _writeRepository;
    private readonly IUtilityService _utilityService;
    private readonly IMediator _mediator;

    public SetMemberPhotoCommandHandler(
        IAzureBlobFileService fileSystemService,
        LazyService<IWriteRepository<object>> writeRepository,
        IUtilityService utilityService,
        IMediator mediator)
    {
        _fileSystemService = fileSystemService;
        _writeRepository = writeRepository;
        _utilityService = utilityService;
        _mediator = mediator;
    }

    public async Task<OperationResultDto<string>> Handle(SetMemberPhotoCommand request, CancellationToken cancellationToken)
    {
        var member = await _mediator.Send(
            new GetMemberBasicInfoBySyncGuidQuery(request.UserSyncId),
            cancellationToken);
        if (member is null)
        {
            return new OperationResultDto<string>
            {
                IsSuccess = false,
                Message = "Member not found.",
                RowsAffected = 0,
                Data = ""
            };
        }

        string uploadedFilePath = await UploadPhoto(request, member, cancellationToken);
        return new OperationResultDto<string>
        {
            IsSuccess = true,
            Message = "Photo uploaded successfully.",
            RowsAffected = 1,
            Data = uploadedFilePath
        };
    }

    private async Task<string> UploadPhoto(SetMemberPhotoCommand request, MemberBasicInfo member, CancellationToken cancellationToken)
    {
        //var sourceImagePath = await _fileSystemService.MapPath(request.Path);
        //var fileDetails = Path.GetFileName(sourceImagePath);
        //var targetImageDirectory = await _fileSystemService.MapPath($"~/Store/User/{member.UserId}/");
        //var targetImagePath = $"{targetImageDirectory}{fileDetails}";

        //await _fileSystemService.CopyFileAsync(sourceImagePath, targetImagePath, cancellationToken);

        //await _fileSystemService.CreateThumbnailAsync(targetImagePath, targetImageDirectory, 50, 50, cancellationToken);

        //string query = @"Update [User] set ProfilePicURL= @ProfilePicUrl where UserId=@UserId";
        //var parameters = new { ProfilePicUrl = fileDetails, UserId = member.UserId };
        //await _writeRepository.Value.ExecuteAsync(query, cancellationToken, parameters, null, "text");

        //await _fileSystemService.DeleteFileAsync(sourceImagePath, cancellationToken);

        ////var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
        ////List<dynamic> audits = new List<dynamic>();
        ////CustomLog.Audit(AuditScheme.UserChanged.Value,
        ////    AuditScheme.UserChanged.BasicDetails.Value,
        ////    AuditScheme.UserChanged.BasicDetails.PictureChanged.Value,
        ////    currentUserId,
        ////    member.UserId,
        ////    EntityType.User,
        ////    member.MemberDocId,
        ////    "Update",
        ////    "User Picture Changed;" + JsonConvert.SerializeObject(audits)
        ////);

        //return $"/store/download?f={fileDetails}&t=user&p={member.UserId}";

        var command = new FileUploadCommand
        {
            File = request.File,
            UserSyncId = request.UserSyncId,
            Module = "User",
            IsThumbnailNeeded = true
        };
        var result = await _mediator.Send(command, cancellationToken);

        string query = @"Update [User] set ProfilePicURL= @ProfilePicUrl where UserId=@UserId";
        var parameters = new { ProfilePicUrl = result.FileName, UserId = member.UserId };
        await _writeRepository.Value.ExecuteAsync(query, cancellationToken, parameters, null, "text");

        return $"/store/download?f={result.FileName}&t=user&p={member.UserId}";
    }
}
