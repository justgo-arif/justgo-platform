namespace JustGo.AssetManagement.Application.DTOs
{
    public class WorkflowResponseDTO
    {
        public bool WorkflowSuccess { get; set; }
        public string Message { get; set; }

        public static WorkflowResponseDTO Ok()
        {

            return new WorkflowResponseDTO()
            {
                WorkflowSuccess = true,
                Message = "Operation Successful."
            };
        }

        public static WorkflowResponseDTO Failed(string message)
        {

            return new WorkflowResponseDTO()
            {
                WorkflowSuccess = false,
                Message = message
            };
        }
    }
}
