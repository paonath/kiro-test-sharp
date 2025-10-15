using Microsoft.EntityFrameworkCore;
using BoltWebAPI.Models.Entities;

namespace BoltWebAPI.Data;

public class BoltDbContext : DbContext
{
    public BoltDbContext(DbContextOptions<BoltDbContext> options) : base(options)
    {
    }

    public DbSet<ExecutionHistoryEntity> ExecutionHistory { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    public DbSet<ConfigurationChangeEntity> ConfigurationChanges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ExecutionHistoryEntity
        modelBuilder.Entity<ExecutionHistoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Command)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.Arguments)
                .IsRequired();
            
            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.State)
                .IsRequired()
                .HasConversion<string>();
            
            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => new { e.UserId, e.StartedAt });
            entity.HasIndex(e => e.State);
        });

        // Configure UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasMaxLength(450);
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired();
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);
            
            // Unique constraint on username
            entity.HasIndex(e => e.Username)
                .IsUnique();
            
            // Unique constraint on email
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            // Index for active users
            entity.HasIndex(e => e.IsActive);
        });

        // Configure RefreshTokenEntity
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(e => e.Token);
            
            entity.Property(e => e.Token)
                .HasMaxLength(500);
            
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);
            
            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for token lookup and cleanup
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.IsRevoked, e.ExpiresAt });
        });

        // Configure ConfigurationChangeEntity
        modelBuilder.Entity<ConfigurationChangeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.PreviousConfiguration)
                .IsRequired();
            
            entity.Property(e => e.NewConfiguration)
                .IsRequired();
            
            // Index for audit queries
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => e.UserId);
        });
    }
}
