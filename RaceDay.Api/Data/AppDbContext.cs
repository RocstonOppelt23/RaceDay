using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core code-first context. Mirrors the Part 1 SQL script exactly:
/// same tables, keys, unique constraints, check constraints and delete rules.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Users ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();

            entity.HasIndex(u => u.Email).IsUnique();                       // UQ_Users_Email
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
        });

        // ---------- UserProfiles : one profile per user ----------
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.ProfileId);
            entity.Property(p => p.PhoneNumber).HasMaxLength(30);
            entity.Property(p => p.ProfilePictureUrl).HasMaxLength(500);

            entity.HasIndex(p => p.UserId).IsUnique();                      // UQ_UserProfiles_UserId

            entity.HasOne(p => p.User)
                  .WithOne(u => u.Profile)
                  .HasForeignKey<UserProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Location).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Distance).HasColumnType("decimal(6,2)");

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.Events)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Events_Distance", "[Distance] > 0");
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
            });
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(500);

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Enrolments ----------
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Active");
            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("SYSUTCDATETIME()");

            // UQ_Enrolments_Participant_Event_Category
            entity.HasIndex(e => new { e.ParticipantId, e.EventId, e.CategoryId }).IsUnique();

            // NoAction on all three: SQL Server rejects multiple cascade paths,
            // and both Users -> Events -> Enrolments and Users -> Enrolments reach this table.
            entity.HasOne(e => e.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Event)
                  .WithMany(ev => ev.Enrolments)
                  .HasForeignKey(e => e.EventId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Enrolments_Status", "[Status] IN ('Active', 'Cancelled')"));
        });

        // ---------- Results : zero or one per enrolment ----------
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasIndex(r => r.EnrolmentId).IsUnique();                 // UQ_Results_EnrolmentId

            entity.HasOne(r => r.Enrolment)
                  .WithOne(e => e.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Results_FinishingPosition", "[FinishingPosition] > 0"));
        });
    }
}
