using FluentValidation;
using JustGo.Authentication.Infrastructure.Files;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.SetMemberPhoto;

public class SetMemberPhotoCommandValidator : AbstractValidator<SetMemberPhotoCommand>
{
    private const long MaxFileSizeInBytes = 25 * 1024 * 1024; // 25 MB
    private static readonly string[] AllowedFileTypes = [".png", ".jpg", ".jpeg", ".webp"];
    public SetMemberPhotoCommandValidator()
    {
        RuleFor(r => r.File).NotNull().NotEmpty().WithMessage("File is required.");
        RuleFor(r => r.File)
            .Must(file => file == null || file.Length <= MaxFileSizeInBytes)
            .WithMessage(file => $"File size ({FileSizeHelper.ToPrettySize(file.File.Length, 2)}) exceeds the maximum allowed size of 25 MB.");

        RuleFor(r => r.File)
            .Must(file => file == null || IsValidFileType(Path.GetExtension(file.FileName).ToLower()))
            .WithMessage(file => $"Unsupported file format: {Path.GetExtension(file.File.FileName)}. Allowed formats: {string.Join(", ", AllowedFileTypes)}.");

        RuleFor(x => x.UserSyncId)
        .NotEmpty()
        .WithMessage("UserSyncId is required.")
        .NotEqual(Guid.Empty)
        .WithMessage("UserSyncId must be a valid GUID.");
    }

    private static bool IsValidFileType(string fileExtension)
    {
        return AllowedFileTypes.Contains(fileExtension);
    }
}
