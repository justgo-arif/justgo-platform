namespace JustGo.Credential.Application.DTOs;

public class CredentialsDto
{
    public int MemberCredentialId { get; set; }
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public required string CredentialsType { get; set; }
    public bool IsLocked { get; set; }
    public int Status { get; set; }
    public string? CredentialCode { get; set; }
    public string? DisclosureNumber { get; set; }
    public string? PaymentDue { get; set; }
    public bool IsNewJourney { get; set; }
    public int CredentialMasterId { get; set; }
    public string? Reference { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RegisterDate { get; set; }
    public int StateId { get; set; }
    public string? StateName { get; set; }
    public int Level { get; set; }
    public decimal Point { get; set; }
    public int NoOfAttachment { get; set; }
    public bool HasNote { get; set; }
}
