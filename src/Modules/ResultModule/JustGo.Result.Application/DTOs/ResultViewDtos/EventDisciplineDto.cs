namespace JustGo.Result.Application.DTOs.ResultViewDtos;

public record EventDisciplineDto
{
    public int DisciplineId { get; init; }
    public required string DisciplineName { get; init; }
}