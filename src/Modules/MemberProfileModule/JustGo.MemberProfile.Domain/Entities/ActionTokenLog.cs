namespace JustGo.MemberProfile.Domain.Entities
{
    public class ActionTokenLog
    {
        public int ActionTokenId { get; set; }
        public string HandlerMessage { get; set; }
        public DateTime InvokedOn { get; set; }
        public string UserAgent { get; set; }
        public string UserHostAddress { get; set; }
        public string UserHostName { get; set; }
        public string UserLanguages { get; set; }
        public string FromDevice { get; set; }
    }
}
