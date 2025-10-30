using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data.Entities;

namespace Migration.WebApp.Infrastructure.Data;

public class MigrationDbContext : DbContext
{
    public MigrationDbContext(DbContextOptions<MigrationDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<MigrationUser> Users { get; set; }
    public DbSet<UserCredential> UserCredentials { get; set; }
    public DbSet<FieldTranslationMapping> FieldMappings { get; set; }
    public DbSet<JiraExportRun> ExportRuns { get; set; }
    public DbSet<JiraExportedItem> ExportedItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Organization relationships
        modelBuilder.Entity<Team>()
            .HasOne(t => t.Organization)
            .WithMany(o => o.Teams)
            .HasForeignKey(t => t.OrganizationId);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Team)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.TeamId);

        // User relationships
        modelBuilder.Entity<MigrationUser>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId);

        modelBuilder.Entity<UserCredential>()
            .HasOne(c => c.User)
            .WithMany(u => u.Credentials)
            .HasForeignKey(c => c.UserId);

        // Configuration relationships
        modelBuilder.Entity<FieldTranslationMapping>()
            .HasOne(f => f.Team)
            .WithMany()
            .HasForeignKey(f => f.TeamId);

        modelBuilder.Entity<FieldTranslationMapping>()
            .HasOne(f => f.Product)
            .WithMany()
            .HasForeignKey(f => f.ProductId);

        // Export relationships
        modelBuilder.Entity<JiraExportRun>()
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId);

        modelBuilder.Entity<JiraExportRun>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);

        modelBuilder.Entity<JiraExportedItem>()
            .HasOne(i => i.ExportRun)
            .WithMany(e => e.ExportedItems)
            .HasForeignKey(i => i.ExportRunId);

        // Indexes for performance
        modelBuilder.Entity<JiraExportedItem>()
            .HasIndex(i => new { i.JiraKey, i.ExportRunId });

        modelBuilder.Entity<FieldTranslationMapping>()
            .HasIndex(f => new { f.TeamId, f.ProductId, f.JiraFieldId });
    }
}