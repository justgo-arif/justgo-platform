using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Enums;
using Newtonsoft.Json;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.CreateUpdate
{
    public class CreateUpdateFinanceGridViewCommandHandler : IRequestHandler<CreateUpdateFinanceGridViewCommand, bool>
    {

        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public CreateUpdateFinanceGridViewCommandHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(CreateUpdateFinanceGridViewCommand request, CancellationToken cancellationToken)
        {

            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            const string sql = @"IF @ViewId IS NULL OR NOT EXISTS (SELECT 1 FROM FinanceGridView WHERE ViewId = @ViewId)
                                    BEGIN
                                        INSERT INTO FinanceGridView
                                        (Name, EntityType, Payload, MerchantId, IsSystemDefault, IsShared, CreatedBy, CreatedDate)
                                        VALUES
                                        (@Name, @EntityType,@Payload, @MerchantId, @IsSystemDefault, @IsShared, @CreatedBy, GETDATE());

                                        SET @ViewId = SCOPE_IDENTITY();
                                    END
                                    ELSE
                                    BEGIN
                                        UPDATE FinanceGridView
                                        SET Name = @Name,
                                            Payload = @Payload,
                                            MerchantId = @MerchantId,
                                            IsSystemDefault = @IsSystemDefault,
                                            IsShared = @IsShared,
                                            UpdatedBy = @CreatedBy,
                                            UpdatedDate = GETDATE()
                                        WHERE ViewId = @ViewId;
                                    END
                                    IF @IsDefault = 1
                                    BEGIN
                                        UPDATE FinanceGridViewPreference
                                        SET IsDefault = 0, UpdatedAt = GETDATE()
                                        WHERE UserId = @UserId;
                                    END
                                    IF EXISTS (SELECT 1 FROM FinanceGridViewPreference WHERE ViewId = @ViewId AND UserId = @UserId)
                                    BEGIN
                                        UPDATE FinanceGridViewPreference
                                        SET IsPinned = @IsPinned,
                                            IsDefault = @IsDefault,
                                            UpdatedAt = GETDATE()
                                        WHERE ViewId = @ViewId AND UserId = @UserId;
                                    END
                                    ELSE
                                    BEGIN
                                        INSERT INTO FinanceGridViewPreference
                                        (ViewId, UserId, IsPinned, IsDefault, CreatedBy, CreatedAt)
                                        VALUES
                                        (@ViewId, @UserId, @IsPinned, @IsDefault,  @UserId, GETDATE());
                                    END";
            var payloadJson = JsonConvert.SerializeObject(request.Payload);
            var parameters = new
            {
                ViewId = request.ViewId,
                UserId = currentUserId,
                Name = request.Name,
                EntityType= request.EntityType,
                Payload = payloadJson,
                MerchantId = request.MerchantId,
                IsSystemDefault = request.IsSystemDefault,
                IsShared = request.IsShared,
                CreatedBy = currentUserId,
                IsPinned = request.IsPinned,
                IsDefault = request.IsSystemDefault
            };

            var rowsAffected = await _writeRepository
           .GetLazyRepository<object>()
           .Value
           .ExecuteAsync(sql, cancellationToken, parameters, dbTransaction, "text");
            await _unitOfWork.CommitAsync(dbTransaction);
            return rowsAffected > 0;

        }
    }
}
