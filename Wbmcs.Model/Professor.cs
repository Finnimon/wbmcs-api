using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace Wbmcs.Model;

public sealed class Professor
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }


    [Key,JsonPropertyName("name"), Required] public string Name { get; set; } = "";
    [JsonPropertyName("faculty")] public string Faculty { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("profile_photo")] public Image Image { get; set; } = new();

    [JsonIgnore] public string Url { get; set; } = "";
    [NotMapped, JsonPropertyName("url")]
    public string FullUrl
    {
        get => FormattableString.Invariant($"{HsbConstants.Url}{Url}");
        set => Url = !value.StartsWith(HsbConstants.Url) ? "" : value[HsbConstants.Url.Length..];
    }

    [JsonIgnore] public Availability Availability { get; set; } = Availability.None;

    [NotMapped, JsonPropertyName("availability_status")]
    public string AvailabilityAsString
    {
        get => AvailabilityStatus.ToString(Availability);
        set => Availability = AvailabilityStatus.Parse(value);
    }

    #region TODO
    [JsonPropertyName("bio")] public string Bio { get; set; } = "";
    [JsonPropertyName("department")] public string Department { get; set; } = "";
    [JsonPropertyName("recent_thesis_topics")] public string RecentThesisTopics { get; set; } = "";
    [JsonPropertyName("research_areas")] public string ResearchAreas { get; set; } = "";
    [JsonPropertyName("supervision_language")] public string SupervisionLanguage { get; set; } = "";
    #endregion


}

public static class HsbConstants
{
    public const string Url = "https://hs-bremen.de";
}

public enum Availability
{
    None,
    Limited,
    Available
}

public static class AvailabilityStatus
{
    public static string ToString(Availability v)
        => v switch
        {
            Availability.Limited => "Limited Availability",
            Availability.Available => "Available",
            Availability.None or _ => "Not Available",
        };

    public static Availability Parse(string v)
        => v switch
        {
            "Limited Availability" => Availability.Limited,
            "Available" => Availability.Available,
            "Not Available" or _ => Availability.None
        };
}
