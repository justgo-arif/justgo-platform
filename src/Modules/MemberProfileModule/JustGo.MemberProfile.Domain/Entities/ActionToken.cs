namespace JustGo.MemberProfile.Domain.Entities
{
    public class ActionToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public string Tag { get; set; }
        public string HandlerType { get; set; }
        public Dictionary<string, string> HandlerArguments { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ExecuteDate { get; set; }
        public ActionTokenStatus Status { get; set; }
        public int InvokeAttempts { get; set; }
        public int VaildFor { get; set; } // in minutes  -- time for which token is valid  compare with CreatedOn
        public Dictionary<string, string> TokenRule { get; set; }
        public List<ActionTokenLog> Items { get; set; }

        public ActionToken()
        {
            TokenRule = new Dictionary<string, string>();
            Items = new List<ActionTokenLog>();
            HandlerArguments = new Dictionary<string, string>();
        }
    }
}
