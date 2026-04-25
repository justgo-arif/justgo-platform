using System.Data.Common;
using System.Text.Json;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.Validators;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands.UpdateEquestrianMemberData;

public class UpdateEaMemberDataProcessor : IUpdateMemberDataProcessor
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEaMemberDataProcessor(IReadRepositoryFactory readRepositoryFactory,
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
        
        //var forComparison = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var currentMemberId = memberDataDict.GetValueOrDefault(ResultUploadFields.MemberId);

        foreach (var kvp in memberDataDict)
        {
            if (!newRequestedDict.TryGetValue(kvp.Key, out var value)) continue;
            
            // if (ResultUploadFields.ValidatableFields.Contains(kvp.Key))
            // {
            //     forComparison[kvp.Key] = kvp.Value;
            // }
            
            memberDataDict[kvp.Key] = value;
        }

        var updatedMemberData = JsonSerializer.Serialize(memberDataDict);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await UpdateMemberDataHelper.UpdateMemberDataAsync(_writeRepositoryFactory, request.Id,
                updatedMemberData, transaction,
                cancellationToken);

            if (newRequestedDict.TryGetValue(ResultUploadFields.MemberId, out var newMemberId) &&
                !string.IsNullOrWhiteSpace(newMemberId))
            {
                if (!string.Equals(newMemberId, currentMemberId, StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateMemberIdAsync(newMemberId, request.Id,
                        transaction,
                        cancellationToken);
                }
            }

            var config = await ImportResultHelper.GetResultUploadFieldValidationConfig(_readRepositoryFactory,
                fileMetaData.DisciplineId, 1, cancellationToken);

            if (!string.IsNullOrEmpty(config))
            {
                IFileDataValidator fileDataValidator = new FileDataValidator(config);
                var validationResult = fileDataValidator.ValidateRow(memberDataDict);

                if (validationResult.Count > 0)
                {
                    var combinedResult = string.Join(", ", validationResult.Select(e => e.ToString()));
                    await UpdateMemberValidationErrorAsync("Validation Failed", combinedResult,
                        fileMetaData.UploadedMemberId, transaction, cancellationToken);
                }
                else
                {
                    await UpdateMemberValidationErrorAsync("Validation Passed", string.Empty,
                        fileMetaData.UploadedMemberId, transaction, cancellationToken);
                }
            }
            
            await UpdateMemberDataHelper.ExecuteValidationAsync(_writeRepositoryFactory, fileMetaData.UploadedMemberId,
                fileMetaData.DisciplineId, transaction,
                cancellationToken);

            // foreach (var kvp in forComparison)
            // {
            //     newRequestedDict.TryGetValue(kvp.Key, out var newValue);
            //     if (string.Equals(kvp.Value, newValue, StringComparison.OrdinalIgnoreCase)) continue;
            //     
            //     
            //     break;
            // }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<string>.Failure($"An error occurred: {ex.Message}", ErrorType.InternalServerError);
        }
    }
    
    private async Task UpdateMemberIdAsync(string newMemberId,
        int memberDataId, DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           UPDATE M
                           SET M.MemberId = @MemberId
                           FROM ResultUploadedMember AS M
                           INNER JOIN ResultUploadedMemberData AS MD
                               ON M.UploadedMemberId = MD.UploadedMemberId
                           WHERE MD.UploadedMemberDataId = @MemberDataId;
                           """;

        var writeRepository = _writeRepositoryFactory.GetRepository<object>();
        await writeRepository.ExecuteAsync(sql, cancellationToken,
            new { MemberId = newMemberId, MemberDataId = memberDataId }, transaction, QueryType.Text);
    }
    
    private async Task<Result<string>> UpdateMemberValidationErrorAsync(
        string errorType, string errorMessage, int uploadedMemberId, DbTransaction transaction, 
        CancellationToken cancellationToken)
    {
        var writeRepository = _writeRepositoryFactory.GetRepository<object>();
        const string updateQuery = """
                                   UPDATE ResultUploadedMember
                                   SET ErrorType = @ErrorType, ErrorMessage = @ErrorMessage
                                   WHERE UploadedMemberId = @UploadedMemberId;
                                   """;
        var rowsAffected = await writeRepository.ExecuteAsync(updateQuery, cancellationToken,
            new { ErrorType = errorType, ErrorMessage = errorMessage, UploadedMemberId = uploadedMemberId },
            transaction, QueryType.Text);

        return rowsAffected > 0
            ? "Error message updated successfully."
            : Result<string>.Failure("Failed to update error message.", ErrorType.InternalServerError);
    }
}