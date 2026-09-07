using Microsoft.EntityFrameworkCore;
using UpdateCenter.Api.Domain;

namespace UpdateCenter.Api.Data;

public class UpdateDbContext : DbContext
{
    public UpdateDbContext(DbContextOptions<UpdateDbContext> options) : base(options)
    {
    }

    public DbSet<AppApplication> Applications => Set<AppApplication>();
    public DbSet<AppRelease> Releases => Set<AppRelease>();
    public DbSet<ReleaseFile> ReleaseFiles => Set<ReleaseFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppApplication>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<AppRelease>(entity =>
        {
            entity.ToTable("Releases");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.ApplicationId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Version).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ReleaseNotes).HasMaxLength(4000);
            entity.Property(x => x.MinimumVersion).HasMaxLength(64);
            entity.Property(x => x.CreatedBy).HasMaxLength(128);
            entity.HasIndex(x => new { x.ApplicationId, x.Version, x.Channel }).IsUnique();
            entity.HasOne(x => x.Application)
                .WithMany(x => x.Releases)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReleaseFile>(entity =>
        {
            entity.ToTable("ReleaseFiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReleaseId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128);
            entity.HasOne(x => x.Release)
                .WithMany(x => x.Files)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
