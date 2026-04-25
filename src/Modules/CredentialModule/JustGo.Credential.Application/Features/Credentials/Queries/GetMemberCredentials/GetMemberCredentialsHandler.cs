using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Credential.Application.DTOs;
using JustGo.MemberProfile.Application.DTOs;
using System;
using System.Text;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetMemberCredentials;

public class GetMemberCredentialsHandler : IRequestHandler<GetMemberCredentialsQuery, OperationResultDto<List<CredentialsDto>>>
{
    private readonly LazyService<IReadRepository<CredentialsDto>> _readRepository;
    public GetMemberCredentialsHandler(LazyService<IReadRepository<CredentialsDto>> readRepository, IMediator mediator)
    {
        _readRepository = readRepository;
    }

    public async Task<OperationResultDto<List<CredentialsDto>>> Handle(GetMemberCredentialsQuery request, CancellationToken cancellationToken)
    {
        return await GetCredentials(request, cancellationToken);
    }

    private async Task<OperationResultDto<List<CredentialsDto>>> GetCredentials(GetMemberCredentialsQuery request, CancellationToken cancellationToken)
    {
        var queryParams = new DynamicParameters();
        queryParams.Add("@UserSyncId", request.UserGuid);

        var (sortSql, joinSql, conditionSql) = AddQueryConditions(request, queryParams);

        var sql = $"""
            WITH UC AS (
                SELECT UC.* 
                FROM UserCredentials UC
                INNER JOIN [User] U ON U.UserId = UC.UserId
                {joinSql}
                WHERE U.UserSyncId = @UserSyncId
                {conditionSql}
            ),
            Attachments AS (
                SELECT UC.CredentialId, COUNT(1) NoOfAttachment
                FROM UC
                INNER JOIN Credentialmaster_Datacaptureitems D ON D.DocId = UC.CredentialMasterId
                CROSS APPLY OPENJSON(D.Config, '$.items') WITH (
                    ExId INT             '$.ExId',
                    FieldId INT          '$.FieldId',
                    Class   VARCHAR(100) '$.Class',
                    ItemId  INT          '$.ItemId'
                ) AS JsonData
                INNER JOIN ExNgbCredential_LargeText A ON A.DocId = UC.CredentialId AND A.FieldId = JsonData.FieldId
                WHERE JsonData.Class = 'MA_Attachment'
                GROUP BY UC.CredentialId
            ),
            Notes AS (
                SELECT UC.CredentialId
                FROM UC
                INNER JOIN MembersCredentials_Memberscredentialnotes N ON N.DocId = UC.CredentialId
                GROUP BY UC.CredentialId
            )
            SELECT
            mcd.DocId MemberCredentialId, mcd.[Name], mcd.[Description], mcd.CredentialsType, mcd.ShortName,
            mcd.IsLocked, mcd.[Status], mcd.CredentialCode, mcd.DisclosureNumber,
            mcd.PaymentDue, mcd.Isnewjourney, CMD.Credentialcategory Category,
            UC.CredentialMasterId, UC.Reference, UC.StartDate, UC.EndDate, UC.RegisterDate,
            ST.StateId, ST.[Name] StateName, CMD.Defaultlength [Level], CMD.Credentialvalue Point,
            A.NoOfAttachment, SIGN(N.CredentialId) HasNote
            FROM UC
            INNER JOIN MembersCredentials_Default MCD ON MCD.DocId = UC.CredentialId
            INNER JOIN Credentialmaster_Default CMD ON CMD.DocId = mcd.CredentialMasterId
            INNER JOIN [State] ST ON ST.StateId = UC.StatusId
            LEFT JOIN Attachments A ON A.CredentialId = UC.CredentialId
            LEFT JOIN Notes N ON N.CredentialId = mcd.DocId
            ;
            """;

        var credentials = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParams, null, commandType: "text")).ToList();

        return new OperationResultDto<List<CredentialsDto>>
        {
            IsSuccess = true,
            Message = "Credentials retrieved successfully.",
            RowsAffected = credentials.Count,
            Data = credentials
        };
    }

    private (string, string, string) AddQueryConditions(GetMemberCredentialsQuery request, DynamicParameters parameters)
    {
        bool isMasterCredentialExist = false, isStatusExist = false;

        string joinSql = "", conditionSql = "", sortSql = "";

        #region SORT & ORDER
        #endregion

        #region FILTERS
        if (!string.IsNullOrWhiteSpace(request.SummaryStatus))
        {
            isStatusExist = true;
            if (request.SummaryStatus.ToLower() == "active")
            {
                conditionSql += " AND ST.[Name] = 'Active'"; //AND UC.EndDate > DATEADD(DAY, 30, CAST(GETDATE() AS DATETIME))
            }
            else if (request.SummaryStatus.ToLower() == "expiring soon")
            {
                conditionSql += " AND ST.[Name] = 'Active' AND UC.EndDate <= DATEADD(DAY, 30, CAST(GETDATE() AS DATETIME))";
                //AND UC.EndDate >= CAST(GETDATE() AS DATETIME)
            }
            else if (request.SummaryStatus.ToLower() == "pending approval")
            {
                conditionSql += " AND ST.[Name] = 'Pending Approval'";
            }
            else if (request.SummaryStatus.ToLower() == "expired")
            {
                conditionSql += " AND ST.[Name] = 'Expired'";
            }
            else if (request.SummaryStatus.ToLower() == "attention required")
            {
                conditionSql += @" AND ST.[Name] IN (
            		'Awaiting Referral',
            		'Awaiting Response',
            		'Inactive Pending Conditions',
            		'Submitted Pending Review'
            	)";
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            isMasterCredentialExist = true;
            parameters.Add("@Category", request.Category);
            conditionSql += " AND CMD.CredentialCategory = @Category";
        }

        if (request.HistoryMode)
        {
            isStatusExist = true;
            conditionSql += " AND ST.[Name] = 'Expired'";
        }
        else
        {
            isStatusExist = true;
            conditionSql += " AND ST.[Name] <> 'Expired'";
        }

        if (request.Level.HasValue)
        {
            isMasterCredentialExist = true;
            parameters.Add("@Level", request.Level.Value);
            conditionSql += " AND CMD.Defaultlength <= @Level";
        }

        if (request.Point.HasValue)
        {
            isMasterCredentialExist = true;
            parameters.Add("@Point", request.Point.Value);
            conditionSql += " AND CMD.CredentialValue <= @Point";
        }

        if (request.Statuses?.Length > 0) 
        {
            string statuses = string.Join(", ", request.Statuses.Select(s => "("+s+")"));
            conditionSql += $@" 
            AND EXISTS (
                SELECT 1 
                FROM (VALUES {statuses}) AS Filter(TargetStatusId)
                WHERE Filter.TargetStatusId = UC.StatusId
            )";
        }

        #endregion

        #region JOINS
        if (isMasterCredentialExist)
        {
            joinSql += @"
                INNER JOIN Credentialmaster_Default CMD ON CMD.DocId = UC.CredentialMasterId
                ";
        }
        
        if (isStatusExist)
        {
            joinSql += @"
            INNER JOIN [State] ST ON ST.StateId = UC.StatusId
            ";
        }

        #endregion

        return (sortSql, joinSql, conditionSql);

    }


}
