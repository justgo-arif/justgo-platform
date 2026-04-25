using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Credential.Application.DTOs;
using JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialsMetaData;
using JustGo.MemberProfile.Application.DTOs;
using System.Data;

public sealed class GetCredentialsMetaDataHandler : IRequestHandler<GetCredentialsMetaDataQuery, OperationResultDto<FilterMetaDataDTO>>
{
    private readonly LazyService<IReadRepository<SelectListItemDTO<int>>> _readRepository;

    public GetCredentialsMetaDataHandler(LazyService<IReadRepository<SelectListItemDTO<int>>> readRepository)
    {
        _readRepository = readRepository;
    }
    public async Task<OperationResultDto<FilterMetaDataDTO>> Handle(GetCredentialsMetaDataQuery request, CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT DISTINCT ST.StateId AS Id, ST.[Name] AS [Text]
            FROM UserCredentials UC
            INNER JOIN [User] U ON U.UserId = UC.UserId
            INNER JOIN [State] ST ON ST.StateId = UC.StatusId
            WHERE U.UserSyncId = @UserSyncId
            ORDER BY ST.[Name];
            """;
        var param = new DynamicParameters();
        param.Add("@UserSyncId", request.Id, DbType.Guid);

        var rows = (await _readRepository.Value.GetListAsync(sql, cancellationToken, param, null, commandType: "text")).ToList();

        var filters = new FilterMetaDataDTO
        {
            StatusList = rows
        };
 
        return new OperationResultDto<FilterMetaDataDTO>
        {
            IsSuccess = true,
            Message = "Credential metadata retrieved successfully.",
            RowsAffected = filters.StatusList.Count,
            Data = filters
        };
    }
}
