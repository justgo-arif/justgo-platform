using JustGoAPI.Shared.Helper;
using Newtonsoft.Json;

namespace JustGo.FieldManagement.Domain.Entities;

public class EntityExtensionOrganisation
{
    public int OwnerId { get; set; }
    public required string OwnerName { get; set; }
    public string OwnerImage { get; set; }
    public required string OwnerType { get; set; }

    #region Field Mgt Columns
    public int ExId { get; set; }
    public int ItemId { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; }
    public string Class { get; set; }

    [JsonConverter(typeof(JsonObjectToStringConverter))]
    public string Config { get; set; }
    public int FieldId { get; set; }
    public int Sequence { get; set; }
    public string SyncGuid { get; set; }
    #endregion
}
