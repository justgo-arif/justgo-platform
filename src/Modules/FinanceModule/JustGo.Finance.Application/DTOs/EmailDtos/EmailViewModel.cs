namespace JustGo.Finance.Application.DTOs.EmailDtos
{
    public class EmailViewModel
    {
        public int EmailId { get; set; }
        public string OwningEntityIdSyncGuid { get; set; }
        public int OwningEntityId { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> Tags { get; set; }
        public string BodyInJson { get; set; }
        public EmailStatus Status { get; set; }
        public int ScheduleTimeZoneId { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime? SentTime { get; set; }
        public bool? IsTemplate { get; set; }
        public int? SegmentId { get; set; }
        public string SegmentDefintion { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public List<EmailOptIn> OptIns { get; set; }
        public List<EmailAttachment> Attachments { get; set; }
        public bool UniqueEmailOnly { get; set; }
        public bool TestSend { get; set; }
        public string Recipient { get; set; }
        public bool ExcludeUnder16 { get; set; }

    }

    public class EmailAttachment
    {
        public int EmailAttachmentId { get; set; }
        public int EmailId { get; set; }
        public string FileName { get; set; }
    }

    public class EmailOptIn
    {
        public int EmailOptInId { get; set; }
        public int EmailId { get; set; }
        public int OptInId { get; set; }
        public bool? IsIncluded { get; set; }
    }

    public enum EmailStatus
    {
        Sending = 0,
        Scheduled = 1,
        Draft = 2
    }
    public enum ActionStatus
    {
        Add = 0,
        Edit = 1,
        Delete = 2
    }

    public class EmailTemplateCategoryModel
    {
        public int CategoryId { get; set; }
        public string OwningEntityIdSyncGuid { get; set; }
        public int OwningEntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ActionStatus? Action { get; set; }
    }

    public class EmailTemplateModel
    {
        public int TemplateId { get; set; }
        public string Name { get; set; }
        public string OwningEntityIdSyncGuid { get; set; }
        public int OwningEntityId { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public string BodyInHtml { get; set; }
        public string BodyInJson { get; set; }
        public string TemplateImage { get; set; }
        public bool IsPremium { get; set; }
        public ActionStatus? Action { get; set; }
    }
}
