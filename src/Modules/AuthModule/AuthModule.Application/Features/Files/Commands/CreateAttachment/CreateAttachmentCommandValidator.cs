using FluentValidation;
using JustGo.Authentication.Infrastructure.Files;
using JustGoAPI.Shared.Helper;

namespace AuthModule.Application.Features.Files.Commands.CreateAttachment;

public class CreateAttachmentCommandValidator : AbstractValidator<CreateAttachmentCommand>
{
    private const long MaxFileSizeInBytes = 25 * 1024 * 1024; // 25 MB
    
    public CreateAttachmentCommandValidator()
    {
        RuleFor(r => r.EntityType).NotEmpty().WithMessage("Entity Type is required.").GreaterThan(0).WithMessage("Entity Type must be greater than zero.");
        RuleFor(r => r.EntityId).NotNull().NotEmpty().WithMessage("Entity Id is required.");
        RuleFor(r => r.Module).NotNull().NotEmpty().WithMessage("Module is required.");
        RuleFor(r => r.File).NotNull().NotEmpty().WithMessage("File is required.");
        RuleFor(r => r.File)
            .Must(file => file == null || file.Length <= MaxFileSizeInBytes)
            .WithMessage(file => $"File size ({FileSizeHelper.ToPrettySize(file.File.Length, 2)}) exceeds the maximum allowed size of 25 MB.");
        RuleFor(r => r.File)
            .Must(file => file == null || Utilities.IsFileAllowed(Path.GetExtension(file.FileName).ToLower()))
            .WithMessage(file => $"Unsupported file format: {Path.GetExtension(file.File.FileName)}.");

    }
}
