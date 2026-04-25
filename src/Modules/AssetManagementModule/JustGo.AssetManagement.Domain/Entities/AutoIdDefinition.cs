namespace JustGo.AssetManagement.Domain.Entities
{
    public class AutoIdDefinition
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string TypePrefix { get; set; }
        public long Current { get; set; }
        public int Length { get; set; }
        public bool PaddingRequired { get; set; }
    }

}
