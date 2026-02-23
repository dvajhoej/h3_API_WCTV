using Microsoft.EntityFrameworkCore;
using WCTV.Api.Models;

namespace WCTV.Api.Data;

public class WctvDbContext : DbContext
{
    public WctvDbContext(DbContextOptions<WctvDbContext> options) : base(options) { }

    public DbSet<Toilet> Toilets => Set<Toilet>();
    public DbSet<ToiletStatus> ToiletStatuses => Set<ToiletStatus>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<CleaningTrigger> CleaningTriggers => Set<CleaningTrigger>();
    public DbSet<CleaningReceipt> CleaningReceipts => Set<CleaningReceipt>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ToiletStatus 1:1 with Toilet (PK = FK)
        modelBuilder.Entity<ToiletStatus>()
            .HasKey(ts => ts.ToiletId);
        modelBuilder.Entity<ToiletStatus>()
            .HasOne(ts => ts.Toilet)
            .WithOne(t => t.Status)
            .HasForeignKey<ToiletStatus>(ts => ts.ToiletId);

        // Session 1-many with Toilet
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Toilet)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.ToiletId);

        // Snapshot 1-many with Session
        modelBuilder.Entity<Snapshot>()
            .HasOne(sn => sn.Session)
            .WithMany(s => s.Snapshots)
            .HasForeignKey(sn => sn.SessionId);

        // Assessment 1:1 with Session
        modelBuilder.Entity<Assessment>()
            .HasIndex(a => a.SessionId)
            .IsUnique();
        modelBuilder.Entity<Assessment>()
            .HasOne(a => a.Session)
            .WithOne(s => s.Assessment)
            .HasForeignKey<Assessment>(a => a.SessionId);

        // CleaningTrigger 1-many with Toilet
        modelBuilder.Entity<CleaningTrigger>()
            .HasOne(ct => ct.Toilet)
            .WithMany(t => t.CleaningTriggers)
            .HasForeignKey(ct => ct.ToiletId);

        // CleaningReceipt 1:1 with CleaningTrigger
        modelBuilder.Entity<CleaningReceipt>()
            .HasIndex(cr => cr.TriggerId)
            .IsUnique();
        modelBuilder.Entity<CleaningReceipt>()
            .HasOne(cr => cr.Trigger)
            .WithOne(ct => ct.Receipt)
            .HasForeignKey<CleaningReceipt>(cr => cr.TriggerId);
    }
}
