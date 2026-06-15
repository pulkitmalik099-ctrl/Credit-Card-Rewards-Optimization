using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<CreditCard> CreditCards { get; set; } = null!;
        public DbSet<RewardCategory> RewardCategories { get; set; } = null!;
        public DbSet<RewardCap> RewardCaps { get; set; } = null!;
        public DbSet<Milestone> Milestones { get; set; } = null!;
        public DbSet<RewardOffer> RewardOffers { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CreditCard configuration
            modelBuilder.Entity<CreditCard>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Issuer).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BaseRewardUnit).HasMaxLength(50);
                entity.Property(e => e.DataSource).HasMaxLength(50);
                
                entity.HasMany(e => e.AcceleratedCategories)
                    .WithOne(c => c.CreditCard)
                    .HasForeignKey(c => c.CreditCardId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(e => e.RewardCaps)
                    .WithOne(c => c.CreditCard)
                    .HasForeignKey(c => c.CreditCardId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(e => e.Milestones)
                    .WithOne(m => m.CreditCard)
                    .HasForeignKey(m => m.CreditCardId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(e => e.Offers)
                    .WithOne(o => o.CreditCard)
                    .HasForeignKey(o => o.CreditCardId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(e => e.Transactions)
                    .WithOne(t => t.CreditCard)
                    .HasForeignKey(t => t.CreditCardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserProfile configuration
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                
                entity.HasMany(e => e.CreditCards)
                    .WithOne(c => c.UserProfile)
                    .HasForeignKey(c => c.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RewardCategory configuration
            modelBuilder.Entity<RewardCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.CreditCardId, e.Category }).IsUnique();
            });

            // RewardCap configuration
            modelBuilder.Entity<RewardCap>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CapType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Unit).HasMaxLength(50);
            });

            // Milestone configuration
            modelBuilder.Entity<Milestone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.RewardUnit).HasMaxLength(50);
            });

            // RewardOffer configuration
            modelBuilder.Entity<RewardOffer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Merchant).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.RewardBasis).HasMaxLength(50);
                entity.Property(e => e.RewardUnit).HasMaxLength(50);
            });

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Merchant).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.CreditCardId, e.TransactionDate });
            });
        }
    }
}
