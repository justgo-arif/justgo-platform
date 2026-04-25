using AuthModule.Domain.Entities;

namespace JustGo.MemberProfile.Application.DTOs
{
    public class ActionTokenHandlerResponse
    {
        public ActionTokenHandlerResponse()
        {
            ViewDatas = new List<KeyValuePair<string, string>>();
            SessionDatas = new List<KeyValuePair<string, string>>();
        }

        public string Token { get; set; }
        public bool Success { get; set; }
        public bool NeverExpire { get; set; }
        public string ErrorMessage { get; set; }
        public string RedirectUrl { get; set; }
        public User LoginUser { get; set; }
        public List<KeyValuePair<string, string>> ViewDatas { get; set; }
        public List<KeyValuePair<string, string>> SessionDatas { get; set; }

        public bool Download { get; set; }
        public string DownloadFile { get; set; }
        public string DownloadName { get; set; }
        public bool DeleteAfterDownload { get; set; }


    }
}
