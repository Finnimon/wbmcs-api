using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using Microsoft.AspNetCore.Mvc;
using Wbmcs.Model;

namespace Wbmcs.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class ProfessorsController : ControllerBase
{
    private Cache.Data? _data;

    private async ValueTask<Cache.Data> GetCachedData()
    {
        if (_data is not null) return _data.Value;
        _data = await Cache.Instance.Unlock();
        return _data.Value;
    }
    [HttpGet(Name = "GetProfessors")]
    public async Task<IActionResult> Get()
    {
        try
        {
            var data = await GetCachedData();
            return Ok(data.Professors);
        }
        catch
        {
            return BadRequest();
        }
    }
    //[HttpGet("{faculty}", Name = "GetProfessorsByFaculty")]
    private readonly char[] _trimChars = " \"'".ToCharArray();
    private async Task<IActionResult> GetFacultyMembers(string faculty)
    {
        var data = await GetCachedData();
        return data.ByFaculty.TryGetValue(faculty, out var val) ? Ok(val) : Empty;
    }

    public readonly record struct FacultyData(string Name, int Count);
    [HttpGet("faculties/", Name = "GetFaculties")]
    public async Task<IActionResult> GetFaculties()
    {
        var data = await GetCachedData();
        return Ok(data.ByFaculty.Select(k => new FacultyData(k.Key, k.Value.Count)));
    }
    [HttpGet("count/", Name = "GetProfessorCount")]
    public async Task<IActionResult> GetProfessorCount()
    {
        var data = await GetCachedData();
        return Ok(data.Professors.Count);
    }

    [HttpGet("search/")]
    public Task<IActionResult> SearchProfessorsGet([FromQuery] string? faculty, [FromQuery] string? name)
        => SearchProfessorsInternal(faculty, name);

    [HttpPost("search/")]
    public Task<IActionResult> SearchProfessorsPost([FromQuery] string? faculty, [FromQuery] string? name)
        => SearchProfessorsInternal(faculty, name);

    private Task<IActionResult> SearchProfessorsInternal(string? faculty, string? name) =>
        (faculty, name) switch
        {
            (not { Length: > 0 }, not { Length: > 0 }) => Get(),
            ({ Length: > 0 }, not { Length: > 0 }) => GetFacultyMembers(faculty.Trim(_trimChars)),
            _ => DoSearch(faculty?.Trim(_trimChars), name.Trim(_trimChars))
        };
    private async Task<IActionResult> DoSearch(string? faculty, string name)
    {
        var data = await GetCachedData();
        if (faculty is null) return SearchByName(name, data.Professors);
        return !data.ByFaculty.TryGetValue(faculty, out var profs) ? Empty : SearchByName(name, profs);
    }

    public IActionResult SearchByName(string name, IEnumerable<EmployeePost.Data> professors) => Ok(SearchEngine.FuzzyLev(professors, prof => prof.Title, name));

    private sealed class Cache
    {
        private EmployeePost.Data[] _professors;
        private Dictionary<string, List<EmployeePost.Data>> _byFaculty;
        private Task? _running;

        public readonly record struct Data(
            IReadOnlyList<EmployeePost.Data> Professors,
            IReadOnlyDictionary<string, List<EmployeePost.Data>> ByFaculty);

        private static readonly Lock s_monitor = new();
        [field: AllowNull, MaybeNull]
        public static Cache Instance
        {
            get
            {
                if (field is not null) return field;
                lock (s_monitor)
                {
                    if (field is not null) return field;
                    return field = new Cache();
                }
            }
        }

        private Cache()
        {
            _professors = [];
            _byFaculty = new Dictionary<string, List<EmployeePost.Data>>(StringComparer.OrdinalIgnoreCase);
            _ = new Timer(Reload, null, new TimeSpan(0, 1, 0, 0), new TimeSpan(1, 0, 0));
            Reload(null);
        }

        public async ValueTask<Data> Unlock()
        {
            var running = _running;
            if (running is null) return GetData();
            await running;
            return GetData();
        }

        private Data GetData() => new(_professors, _byFaculty);


        private void Reload(object? _)
        {
            var entered = System.Threading.Monitor.TryEnter(this);
            TaskCompletionSource s = new();
            _running = s.Task;
            try
            {
                var allData = EmployeePost.LoadAllEmployees().GetAwaiter().GetResult(); ;
                var professors = allData.Data.Where(d => d.IsProfessor)
                    .ToArray();
                var byFaculty = new Dictionary<string, List<EmployeePost.Data>>(StringComparer.OrdinalIgnoreCase);
                foreach (var professor in professors)
                {
                    if (professor.Faculty.Length == 0)
                    {
                        AddEntry("UNKNOWN", professor, byFaculty);
                        continue;
                    }
                    foreach (var faculty in professor.Faculty)
                        AddEntry(faculty, professor, byFaculty);

                }

                _professors = professors;
                _byFaculty = byFaculty;
            }
            catch
            {
                //ignore
            }
            finally
            {
                s.SetResult();
                _running = null;
                if (entered) System.Threading.Monitor.Exit(this);
            }
        }

        private static void AddEntry(string key, EmployeePost.Data professor,
            Dictionary<string, List<EmployeePost.Data>> byFaculty)
        {
            if (!byFaculty.TryGetValue(key, out var l))
            {
                l = [];
                byFaculty[key] = l;
            }

            l.Add(professor);
        }

    }
}
