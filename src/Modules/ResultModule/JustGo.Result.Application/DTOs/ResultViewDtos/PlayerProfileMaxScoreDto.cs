namespace JustGo.Result.Application.DTOs.ResultViewDtos;

public class PlayerProfileMaxScoreDto
{
    public string MemberId { get; set; }
    public string UserName { get; set; }
    public string County { get; set; }
    public string Country { get; set; }
    public string Gender { get; set; }
    public DateTime? DOB { get; set; }
    public string ClubName { get; set; }
    public string DisciplineName { get; set; }
    public string PlayerImageUrl { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Age
    {
        get
        {
            if (DOB == null)
                return 0.0m;

            var dob = DOB.Value.Date;
            var today = DateTime.Today;

            int years = today.Year - dob.Year;
            int months = today.Month - dob.Month;

            if (today.Day < dob.Day)
            {
                months--;
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }
            return years + (months / 100m);
        }
    }

    public List<PlayerProfileDisciplineScoreDto> Items { get; set; } = new();
    public List<string> SocialNetworks { get; set; } = new();
}

public class PlayerProfileDisciplineScoreDto
{
    public string DisciplineName { get; set; }
    public decimal MaxScore { get; set; }
}


