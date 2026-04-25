namespace JustGo.Result.Application.Common.Enums;

public enum FileStatus : byte
{
    Evaluating = 1,
    Cancelled = 2, // When User Cancel the Upload
    PendingReview = 3, //On Successfully Upload
    Completed = 4, // After Admin Review and Approve - Need a separate API
    Archived = 5, // After Admin Review and Reject - Need a separate API,
    Failed = 6, // On any error during upload or processing,
    Unarchived = 7, // When Admin Unarchive the file - Need a separate API
    Revalidating = 8 // When Admin Revalidating the file - Need a separate API
}

public enum ResultCompetitionStatus : byte
{
    Draft = 1,
    Published = 2,
    InProgress = 3,
    Failed = 4
}