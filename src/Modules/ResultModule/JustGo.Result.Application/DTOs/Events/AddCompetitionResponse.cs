namespace JustGo.Result.Application.DTOs.Events;

    public class AddCompetitionResponse
    {
        public int EventId { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }




