using JustGo.Authentication.Services.Interfaces.CustomMediator;

public class GetCompetitionMetadataQuery : IRequest<CompetitionCreateMetadataDto>
{
    public string? OwnerGuid { get; set; }
}