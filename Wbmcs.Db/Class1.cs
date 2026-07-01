using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wbmcs.Model;
using static System.Net.Mime.MediaTypeNames;

namespace Wbmcs.Db;

public sealed class UniversityDbContext : DbContext
{
    public DbSet<Professor> Professors => Set<Professor>();
    public DbSet<PublicationAbstract> Abstracts => Set<PublicationAbstract>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Professor>()
            .HasMany(x => x.Abstracts)
            .WithOne(x => x.Professor)
            .HasForeignKey(x => x.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
public sealed class Professor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [StringLength(256)] public string Title { get; set; } = null!;
    [StringLength(256)] public string? Position { get; set; }
    [StringLength(256)] public string? Email { get; set; }
    [StringLength(256)] public string? Phone { get; set; }
    [StringLength(256)] public string? Mobile { get; set; }
    [StringLength(256)] public string? Url { get; set; }

    // Store image separately if large
    public EmployeePost.Image? Image { get; set; }

    public ICollection<string> Faculties { get; set; }
        = new List<string>();

    public ICollection<PublicationAbstract> Abstracts { get; set; }
        = new List<PublicationAbstract>();
}

public sealed class PublicationAbstract
{
    public Guid Id { get; set; }

    public int ProfessorId { get; set; }

    public Professor Professor { get; set; } = null!;

    [StringLength(1024)] public string Title { get; set; } = null!;

    public string AbstractText { get; set; } = null!;

    [StringLength(512)] public string? PublicationUrl { get; set; }

    public DateTime? PublishedAt { get; set; }
}


public sealed class ProfessorSyncInterceptor(IAbstractImporter importer) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken ct = default)
    {
        var context = eventData.Context!;
        var addedProfessors =
            context.ChangeTracker
                .Entries<Professor>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity);

        foreach (var professor in addedProfessors)
        await foreach (var abs in importer.ImportAbstracts(professor, ct))
            professor.Abstracts.Add(abs);
        return result;
    }
}

public interface IAbstractImporter
{
    public IAsyncEnumerable<PublicationAbstract> ImportAbstracts(Professor prof, CancellationToken t);
}
