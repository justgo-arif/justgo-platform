namespace JustGo.Credential.Application.DTOs;

public class CredentialSummaryDto
{
    public int ActiveCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int AttentionRequiredCount { get; set; }
    public int ExpiredCount { get; set; }
}
