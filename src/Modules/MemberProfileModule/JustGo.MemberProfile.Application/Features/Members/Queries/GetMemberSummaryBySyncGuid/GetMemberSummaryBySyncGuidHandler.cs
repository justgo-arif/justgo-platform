using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Domain.Entities;
using MapsterMapper;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberSummaryBySyncGuid;

public class GetMemberSummaryBySyncGuidHandler : IRequestHandler<GetMemberSummaryBySyncGuidQuery, MemberSummaryDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    public GetMemberSummaryBySyncGuidHandler(IReadRepositoryFactory readRepository, IMapper mapper, IMediator mediator)
    {
        _readRepository = readRepository;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<MemberSummaryDto?> Handle(GetMemberSummaryBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        var summary = await GetMemberSummary(request, cancellationToken);
        if (summary is null)
            return null;

        var result = MapToDto(summary);
        //result.Family = await _mediator.Send(new GetFamilySummaryQuery(request.Id), cancellationToken);
        return result;
    }

    private async Task<MemberSummary?> GetMemberSummary(GetMemberSummaryBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        string sql = """
                    SELECT TOP 1 U.UserId, U.MemberDocId, U.FirstName, U.LastName, P.Number Mobile, P.CountryCode,
                    U.CreationDate, U.LastLoginDate, U.IsActive, U.IsLocked, U.EmailAddress, U.ProfilePicURL, 
                    U.DOB, U.Gender, U.Address1, U.Address2, U.Address3, U.Town, U.County, U.Country, U.PostCode, U.EmailVerified, U.MemberId, U.UserSyncId, 
                    U.SuspensionLevel, U.LoginId, MD.Tempcategory PrimaryMembership, U.CountryId, U.CountyId
                    FROM [User] U
                    INNER JOIN Members_Default MD ON MD.DocId = U.MemberDocId
                    OUTER APPLY (
                        SELECT TOP 1 Number, CountryCode
                        FROM UserPhoneNumber
                        WHERE UserId = U.Userid AND [Type] = 'Mobile'
                        ORDER BY Id ASC
                    ) P
                    WHERE U.UserSyncId = @UserSyncId;
                    """;
        return await _readRepository.GetLazyRepository<MemberSummary>().Value.GetAsync(sql, cancellationToken, new { UserSyncId = request.Id }, null, "text");
    }

    private MemberSummaryDto MapToDto(MemberSummary summary)
    {
        var summaryDto = _mapper.Map<MemberSummaryDto>(summary);
        summaryDto.ProfilePicURL = GetProfilePath(summary.ProfilePicURL, summary.UserId);
        return summaryDto;
    }

    private string? GetProfilePath(string? ProfilePicURL, int userId)
    {
        if (string.IsNullOrWhiteSpace(ProfilePicURL))
            return null;

        return $"/store/download?f={ProfilePicURL}&t=user&p={userId}";
    }

}
