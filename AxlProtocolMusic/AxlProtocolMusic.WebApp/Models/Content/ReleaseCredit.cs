using MongoDB.Bson.Serialization.Attributes;

namespace AxlProtocolMusic.WebApp.Models.Content;

[BsonIgnoreExtraElements]
public sealed class ReleaseCredit
{
    public string Name { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];

    [BsonElement("Role")]
    [BsonIgnoreIfNull]
    public string? LegacyRole
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value)
                && !Roles.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                Roles.Add(value);
            }
        }
    }
}
