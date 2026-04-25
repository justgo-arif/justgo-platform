using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.EmailServices  
{
    public  class MailQueueItemDataReaderService
    {
        public int IdColumn = -1;
        public int SenderColumn = -1;
        public int ToColumn = -1;
        public int CCColumn = -1;
        public int BccColumn = -1;
        public int SubjectColumn = -1;
        public int MailBodyColumn = -1;

        public int AttachmentsPathColumn = -1;
        public int FailCountColumn = -1;
        public int StatusColumn = -1;
        public int ErrorMessageColumn = -1;
        public int CreatedDateColumn = -1;
        public int SentDateColumn = -1;
        public int TagColumn = -1;
        public int MessageIdColumn = -1;
        public int PageSelectionColumn = -1;

        public int EmailTypeColumn = -1;
        public int ReplyToEmailAddressColumn = -1;
        public int ReplyToNameColumn = -1;

        public MailItem Read(MailItem data)
        {
            var mailItem = new MailItem
            {
                Id = Convert.ToInt32(data.Id),
                Sender = GetString(data.Sender),
                To = GetString(data.To),
                CC = GetString(data.CC),
                Bcc = GetString(data.Bcc),
                Subject = GetString(data.Subject),
                Mailbody = GetString(data.Mailbody),
                AttachmentsPath = GetString(data.AttachmentsPath),
                FailCount = GetInt(data.FailCount),
                Status = GetInt(StatusColumn),
                ErrorMessage = GetString(data.ErrorMessage),
                CreatedDate = GetDate(data.CreatedDate),
                SentDate = GetDate(data.SentDate),
                Tag = GetString(data.Tag),
                MessageId = GetInt(data.MessageId),
                PageSelection = GetString(data.PageSelection),
                EmailType = GetInt(EmailTypeColumn),
                ReplyToEmailAddress = GetString(data.ReplyToEmailAddress),
                ReplyToName = GetString(data.ReplyToName)

            };
            return mailItem;
        }

        public List<MailItem> ResolveOrdinal(List<MailItem> dataList)        
        {

            var matchedFields = new List<MailItem>();

            foreach (var item in dataList)
            {
                matchedFields.Add(Read(item));
            }

            return matchedFields;
        }


        public int GetInt(int ordinal)
        {
            if (ordinal == -1 || ordinal is 0) return int.MinValue;
            return Convert.ToInt32(ordinal);
        }
        public string GetString(string ordinal)
        {
            if (ordinal == "-1" || string.IsNullOrEmpty(ordinal)) return string.Empty;
            return Convert.ToString(ordinal);
        }
        public DateTime GetDate(DateTime data)
        {
            if (IsDateEmpty(data)) return DateTime.MinValue;
            return data;
        }
        public static bool IsDateEmpty(DateTime date)
        {
            return date == DateTime.MinValue;
        }

        public static bool IsDateEmpty(DateTime? date)
        {
            return !date.HasValue || date.Value == DateTime.MinValue;
        }
    }
    
}
