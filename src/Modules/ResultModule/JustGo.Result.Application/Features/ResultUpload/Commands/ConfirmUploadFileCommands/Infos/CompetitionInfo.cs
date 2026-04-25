using System.Data;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;

public record CompetitionInfo(
    string? ClassCategoryName,
    string? ClassName,
    int DisciplineId,
    int EventId,
    int UploadedFileId,
    DateTime StartDate,
    DateTime EndDate,
    string AdditionalData);

public record RepositoryContext(
    IReadRepository<object> ReadRepo,
    IWriteRepository<object> WriteRepo,
    IDbTransaction Transaction);