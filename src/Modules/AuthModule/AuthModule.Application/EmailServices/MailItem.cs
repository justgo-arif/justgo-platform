using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.EmailServices  
{
    public class MailItem
    {
        public int Id
        {
            get;
            set;
        }
        public string Sender
        {
            get;
            set;
        }
        public string To
        {
            get;
            set;
        }
        public string CC
        {
            get;
            set;
        }
        public string Bcc
        {
            get;
            set;
        }

        public string Subject
        {
            get;
            set;
        }
        public string Mailbody
        {
            get;
            set;
        }

        public string AttachmentsPath
        {
            get;
            set;
        }
        public int FailCount
        {
            get;
            set;
        }


        public int Status
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }

        public string PageSelection
        {
            get;
            set;
        }

        public DateTime CreatedDate { get; set; }
        public DateTime SentDate { get; set; }
        public string Tag
        {
            get;
            set;
        }

        public int MessageId
        {
            get;
            set;
        }
        public int EmailType
        {
            get;
            set;
        }

        public string ReplyToEmailAddress
        {
            get;
            set;
        }
        public string ReplyToName
        {
            get;
            set;
        }
    }
}
