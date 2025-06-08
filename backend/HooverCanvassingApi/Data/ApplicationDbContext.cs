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
        }
    }
}