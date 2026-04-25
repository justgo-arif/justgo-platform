using FluentValidation;
using JustGo.Authentication.Infrastructure.Files;
using JustGoAPI.Shared.Helper;

namespace AuthModule.Application.Features.Files.Commands.FileUpload;

internal class FileUploadCommandValidator : AbstractValidator<FileUploadCommand>
{
    private const long MaxFileSizeInBytes = 25 * 1024 * 1024; // 25 MB

    public FileUploadCommandValidator()
    {
        RuleFor(r => r.File).NotNull().NotEmpty().WithMessage("File is required.");
        RuleFor(r => r.File)
            .Must(file => file == null || file.Length <= MaxFileSizeInBytes)
            .WithMessage(file => $"File size ({FileSizeHelper.ToPrettySize(file.File.Length, 2)}) exceeds the maximum allowed size of 25 MB.");
        RuleFor(r => r.File)
            .Must(file => file == null || Utilities.IsFileAllowed(Path.GetExtension(file.FileName).ToLower()))
            .WithMessage(file => $"Unsupported file format: {Path.GetExtension(file.File.FileName)}.");

    }
}
