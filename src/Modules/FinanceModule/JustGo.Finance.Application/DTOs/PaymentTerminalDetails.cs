namespace JustGo.Finance.Application.DTOs
{
    public class PaymentTerminalDetails
    {
        public string StatementDescriptor { get; set; } = string.Empty;
        public string TransferDestination { get; set; } = string.Empty;
        public string Transfer { get; set; } = string.Empty;
        public string CollectedFeeURL { get; set; } = string.Empty;
        public decimal CollectedFee { get; set; }   
        public string TransferGroup { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
