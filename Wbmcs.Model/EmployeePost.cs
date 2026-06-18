using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Wbmcs.Model;

public static class EmployeePost
{
    //{
    //   "type": "tt_address",
    //   "contentType": "Person",
    //   "title": "Prof. Dr. Stephan  Ab\u00e9e",
    //   "position": "Professur f\u00fcr Betriebswirtschaftslehre",
    //   "email": "Stephan.Abee@hs-bremen.de",
    //   "phone": "+49 421 5905 4130",
    //   "mobile": null,
    //   "image": {
    //     "src": "\/assets\/hsb\/de\/_processed_\/0\/3\/csm_Abee_Stephan-0682-1600px_74795098ca.jpg",
    //     "alt": "Auf dem Bild ist Stephan Ab\u00e9e zu sehen. Er hat welliges braunes Haar und einen braunen Bart. Er tr\u00e4gt ein wei\u00dfes Hemd mit einem leichten Rosastich. ",
    //     "title": "Stephan Ab\u00e9e"
    //   },
    //   "url": "\/person\/sabee\/"
    // }

    [field: AllowNull, MaybeNull]
    private static HttpClient Client => field ??= new HttpClient();
    public static async Task<(Data[] Data, string[] Faculties)> LoadAllEmployees()
    {
        var faculties = await GetFaculties();
        ConcurrentDictionary<string, (EmployeeDirectData dat, string[] faculties)> allData = new(StringComparer.OrdinalIgnoreCase);
        var tasks=faculties.Select(faculty => Task.Run(async () =>
            {
                var documents = await GetEmployeeApiDocuments(1, faculty);
                var pagination = documents.GetProperty("pagination").Deserialize<Pagination>(SerializerOptions)!;
                var remPages = pagination.NumberOfPages - 1;
                var additionalDocs = await Task.WhenAll(Enumerable.Range(2, remPages)
                    .Select(i => GetEmployeeApiDocuments(i, faculty)));
                var facultyData = additionalDocs.Prepend(documents).SelectMany(ReadEmployeeList);
                string[] facultiesArr = [faculty];
                foreach (var employeeDirectData in facultyData)
                {
                    if (allData.TryAdd(employeeDirectData.Title, (employeeDirectData, facultiesArr)))
                        continue;
                    allData[employeeDirectData.Title] = (employeeDirectData,
                        [.. allData[employeeDirectData.Title].faculties, faculty]);
                }
            }
        )).ToArray();
        await Task.WhenAll(tasks);
        return (allData.Values.Select(entry=>Parse(entry.dat,entry.faculties)).ToArray(), faculties);
    }

    private sealed class DataComp : EqualityComparer<Data>
    {
        public override bool Equals(Data? x, Data? y)
        {
            if (x is null) return y is null;
            return y is not null && x!.Title.Equals(y.Title);
        }

        public override int GetHashCode(Data obj) => obj.Title.GetHashCode();
    }
    private static EmployeeDirectData[] ReadEmployeeList(JsonElement documents)
    {
        var list = documents.GetProperty("list");
        var listCount = int.Parse(list.GetProperty("count").GetString()!);
        if (listCount == 0)
            return [];
        var listElems = list.GetProperty("results").Deserialize<EmployeeDirectData[]>(SerializerOptions)!;
        Debug.Assert(listElems.Length == listCount);
        return listElems;
    }

    private static Task<JsonElement> GetEmployeeApiDocuments(int page = 1) => GetEmployeeApiDocuments(page, null);
    private static async Task<JsonElement> GetEmployeeApiDocuments(int page, string? faculty)
    {
        var hsbApi = HsbApi;
        var content = new MultipartFormDataContent();
        if (!string.IsNullOrEmpty(faculty))
            content.Add(
                new StringContent(FormattableString.Invariant($"faculty:{faculty}")),
                "search[filter][]"
            );
        content.Add(
            new StringContent(page.ToString(CultureInfo.InvariantCulture)),
            "search[page]"
        );
        using var response = await Client.PostAsync(hsbApi, content);
        await using var responseData = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(responseData);
        var enumerator = doc.RootElement
            .GetProperty("content")
            .GetProperty("colPos0")
            .EnumerateArray()
            .ToArray();
        Debug.Assert(enumerator.Length == 1);
        var col = enumerator[0];

        var documents = col.GetProperty("content")
            .GetProperty("data")
            .GetProperty("documents");
        return documents.Clone();
    }

    [field: AllowNull, MaybeNull]
    private static Uri HsbApi => field ??= new("https://www.hs-bremen.de/api/v1/search/employees/");

    private static async Task<string[]> GetFaculties()
    {
        using var response = await Client.PostAsync(HsbApi, null);
        var responseData = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(responseData);
        var enumerator = doc.RootElement
            .GetProperty("content")
            .GetProperty("colPos0")
            .EnumerateArray()
            .ToArray();
        Debug.Assert(enumerator.Length == 1);
        var col = enumerator[0];

        return col.GetProperty("content")
            .GetProperty("data")
            .GetProperty("facets")
            .GetProperty("middle")
            .EnumerateArray()
            .Where(elem => elem.TryGetProperty("name", out var name)
                           && name.GetString() is "faculty")
            .Select(elem => elem.GetProperty("options"))
            .SelectMany(options => options.EnumerateArray())
            .Select(opt => opt.TryGetProperty("value", out var val) ? val.GetString() : null)
            .Select(s => s ?? "")
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();
    }
    private static JsonSerializerOptions SerializerOptions => field ??= new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    public sealed record Image(string Src, string? Alt, string? Title);
    private sealed record EmployeeDirectData(
        string TtAddress,
        string ContentType,
        string Title,
        string? Position,
        string? Email,
        string? Phone,
        string? Mobile,
        Image? Image,
        string? Url
        );
    public sealed record Data(
        string Title,
        bool IsProfessor,
        string? Position,
        string? Email,
        string? Phone,
        string? Mobile,
        Image? Image,
        string? Url,
        string[] Faculty
    );

    private static Data Parse(EmployeeDirectData src, string[] faculty)
        => new(
            src.Title,
            src.Position?.StartsWith("Professur") is true || src.Title.StartsWith("Prof."),
            src.Position,
            src.Email,
            src.Phone,
            src.Mobile,
            src.Image,
            src.Url,
            faculty
        );
    private sealed record Pagination(int Current, int? Previous, int? Next, int NumberOfPages);
}