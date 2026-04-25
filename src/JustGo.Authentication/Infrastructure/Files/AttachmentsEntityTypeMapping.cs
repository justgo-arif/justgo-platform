namespace JustGo.Authentication.Infrastructure.Files
{
    public class AttachmentsEntityTypeMapping
    {
        public int MappingId { get; set; }
        public int EntityTypeId { get; set; }
        public string EntityTypeName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string GuidColumn { get; set; } = string.Empty;
        public string IdColumn { get; set; } = string.Empty;
    }
}
