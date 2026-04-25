using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateStatementDescriptor;

public class UpdateStatementDescriptorHandler : IRequestHandler<UpdateStatementDescriptorCommand, bool>
{
    private readonly LazyService<IWriteRepository<string>> _writeRepository;
    private readonly ICustomError _error;

    public UpdateStatementDescriptorHandler(LazyService<IWriteRepository<string>> writeRepository, ICustomError error)
    {
        _writeRepository = writeRepository;
        _error = error;
    }

    public async Task<bool> Handle(UpdateStatementDescriptorCommand request, CancellationToken cancellationToken)
    {
        var query = @"
                DECLARE @MerchantId INT = (
                    SELECT docid
                    FROM document
                    WHERE syncguid = @MerchantGuid
                );

                UPDATE AdyenAccounts
                SET 
                    StatementDescriptor = @statementDescriptor,
                    UpdatedDate = GETUTCDATE()
                WHERE 
                    EntityId = @MerchantId;
                ";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@MerchantGuid", request.MerchantId);
        queryParameters.Add("@statementDescriptor", request.StatementDescriptor);

        var rowsAffected = await _writeRepository.Value.ExecuteAsync(query, cancellationToken, queryParameters, null, "text");
        if (rowsAffected == 0)
        {
            _error.NotFound<object>("Merchant not found or no update was made.");
            return false;
        }
        return rowsAffected > 0;
    }
}
