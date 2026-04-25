namespace AuthModule.Domain.Entities.MFA;

public class IsActionAllowQueryModel
{
    public Guid UserGuid { get; set; }
}
public class IsActionAllowQueryModel_V2
{
    public int MemberDocId { get; set; }
    public int UserId { get; set; }
}