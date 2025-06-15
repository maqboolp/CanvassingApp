using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<Volunteer>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Voter> Voters { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Volunteer> Volunteers => Set<Volunteer>();
        public DbSet<InvitationToken> InvitationTokens { get; set; }
        public DbSet<PendingVolunteer> PendingVolunteers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Voter entity
            builder.Entity<Voter>(entity =>
            {
                entity.HasKey(v => v.LalVoterId);
                entity.Property(v => v.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(v => v.LastName).IsRequired().HasMaxLength(100);
                entity.Property(v => v.AddressLine).IsRequired().HasMaxLength(500);
                entity.Property(v => v.City).IsRequired().HasMaxLength(100);
                entity.Property(v => v.State).IsRequired().HasMaxLength(50);
                entity.Property(v => v.Zip).IsRequired().HasMaxLength(10);
                entity.Property(v => v.Gender).IsRequired().HasMaxLength(20);
                entity.Property(v => v.VoteFrequency).HasConversion<string>();
                entity.Property(v => v.LastContactStatus).HasConversion<string>();
                entity.HasIndex(v => v.Zip);
                entity.HasIndex(v => v.VoteFrequency);
                entity.HasIndex(v => v.IsContacted);
            });

            // Configure Volunteer entity
            builder.Entity<Volunteer>(entity =>
            {
                entity.Property(v => v.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(v => v.LastName).IsRequired().HasMaxLength(100);
                entity.Property(v => v.Role).HasConversion<string>();
            });

            // Configure Contact entity
            builder.Entity<Contact>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Status).HasConversion<string>();
                entity.HasIndex(c => c.Timestamp);
                entity.HasIndex(c => c.VoterId);
                entity.HasIndex(c => c.VolunteerId);
            });

            // Configure relationships
            builder.Entity<Contact>()
                .HasOne(c => c.Voter)
                .WithMany(v => v.Contacts)
                .HasForeignKey(c => c.VoterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contact>()
                .HasOne(c => c.Volunteer)
                .WithMany(v => v.Contacts)
                .HasForeignKey(c => c.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure InvitationToken entity
            builder.Entity<InvitationToken>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Email).IsRequired().HasMaxLength(255);
                entity.Property(i => i.Token).IsRequired().HasMaxLength(255);
                entity.Property(i => i.Role).HasConversion<string>();
                entity.HasIndex(i => i.Token).IsUnique();
                entity.HasIndex(i => i.Email);
                entity.HasIndex(i => i.ExpiresAt);
            });

            // Configure PendingVolunteer entity
            builder.Entity<PendingVolunteer>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Email).IsRequired().HasMaxLength(255);
                entity.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(p => p.RequestedRole).HasConversion<string>();
                entity.Property(p => p.Status).HasConversion<string>();
                entity.HasIndex(p => p.Email).IsUnique();
                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.CreatedAt);
            });

            // Configure InvitationToken relationships
            builder.Entity<InvitationToken>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<InvitationToken>()
                .HasOne(i => i.CompletedBy)
                .WithMany()
                .HasForeignKey(i => i.CompletedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure PendingVolunteer relationships
            builder.Entity<PendingVolunteer>()
                .HasOne(p => p.ReviewedBy)
                .WithMany()
                .HasForeignKey(p => p.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}