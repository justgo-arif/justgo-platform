namespace JustGo.MemberProfile.Application.DTOs;

public class OperationResultDto
{
    public bool IsSuccess { get; set; }
    public required string Message { get; set; }
    public int RowsAffected { get; set; }
}

public class OperationResultDto<T> : OperationResultDto
{
    public T? Data { get; set; }
}