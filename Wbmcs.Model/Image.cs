using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Wbmcs.Model;

public sealed class Image
{
    public Image() : this("", null, null) { }
    public Image(string url,
        string? alt,
        string? title)
    {
        Url = url;
        Alt = alt;
        Title = title;
    }

    [JsonPropertyName("url"),Required] public string Url { get; init; }
    [JsonPropertyName("alt")] public string? Alt { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }

    public void Deconstruct(out string Url, out string? Alt, out string? Title)
    {
        Url = this.Url;
        Alt = this.Alt;
        Title = this.Title;
    }
}