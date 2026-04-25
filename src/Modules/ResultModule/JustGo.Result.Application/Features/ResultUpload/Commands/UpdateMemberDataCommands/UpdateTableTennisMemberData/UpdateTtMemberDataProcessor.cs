using System.Text.Json;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands.UpdateTableTennisMemberData;

public class UpdateTtMemberDataProcessor : IUpdateMemberDataProcessor
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTtMemberDataProcessor(IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> ProcessAsync(UpdateMemberDataCommand request, CancellationToken cancellationToken)
    {
        var fileMetaData = await UpdateMemberDataHelper.GetFileMetaDataAsync(_readRepositoryFactory, request.Id, 
            cancellationToken);

        if (await UpdateMemberDataHelper.IsFileConfirmedAsync(_readRepositoryFactory, fileMetaData.UploadedFileId,
                cancellationToken))
        {
            return Result<string>.Failure("Cannot update member data for a confirmed file.", ErrorType.Conflict);
        }

        var memberDataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(fileMetaData.MemberData);
        if (memberDataDict == null || memberDataDict.Count == 0)
        {
            return Result<string>.Failure("Member data is empty or invalid.", ErrorType.BadRequest);
        }

        var newRequestedDict = new Dictionary<string, string>(request.DynamicProperties,
            StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in memberDataDict)
        {
            if (newRequestedDict.TryGetValue(kvp.Key, out var value))
            {
                memberDataDict[kvp.Key] = value;
            }
        }

        var updatedMemberData = JsonSerializer.Serialize(memberDataDict);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await UpdateMemberDataHelper.UpdateMemberDataAsync(_writeRepositoryFactory, request.Id,
                updatedMemberData, transaction,
                cancellationToken);

            await UpdateMemberDataHelper.ExecuteValidationAsync(_writeRepositoryFactory, fileMetaData.UploadedMemberId,
                fileMetaData.DisciplineId, transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<string>.Failure($"An error occurred: {ex.Message}", ErrorType.InternalServerError);
        }
    }
}