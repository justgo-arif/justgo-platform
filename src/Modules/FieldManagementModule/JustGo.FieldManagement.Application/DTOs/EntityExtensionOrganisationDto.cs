using JustGoAPI.Shared.Helper;
using Newtonsoft.Json;

namespace JustGo.FieldManagement.Application.DTOs;

public class EntityExtensionOrganisationDto
{
    public int OwnerId { get; set; }
    public required string OwnerName { get; set; }
    public string OwnerImage { get; set; }
    public required string OwnerType { get; set; }
    public List<EntityExtensionOrganisationItemDto> Items { get; set; } = new();
}

public class EntityExtensionOrganisationItemDto
{
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
}