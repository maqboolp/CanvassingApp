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
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignMessage> CampaignMessages { get; set; }
        public DbSet<VolunteerResource> VolunteerResources { get; set; }
        public DbSet<VoterTag> VoterTags { get; set; }
        public DbSet<VoterTagAssignment> VoterTagAssignments { get; set; }
        public DbSet<ConsentRecord> ConsentRecords { get; set; }

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
                entity.Property(v => v.VoterSupport).HasConversion<string>();
                entity.Property(v => v.SmsConsentStatus).HasConversion<string>();
                entity.Property(v => v.SmsOptInMethod).HasConversion<string>();
                entity.HasIndex(v => v.Zip);
                entity.HasIndex(v => v.VoteFrequency);
                entity.HasIndex(v => v.IsContacted);
                entity.HasIndex(v => v.SmsConsentStatus);
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

            // Configure Campaign entity
            builder.Entity<Campaign>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Message).IsRequired().HasMaxLength(1600); // SMS limit
                entity.Property(c => c.Type).HasConversion<string>();
                entity.Property(c => c.Status).HasConversion<string>();
                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.CreatedAt);
                entity.HasIndex(c => c.ScheduledTime);
            });

            // Configure CampaignMessage entity
            builder.Entity<CampaignMessage>(entity =>
            {
                entity.HasKey(cm => cm.Id);
                entity.Property(cm => cm.RecipientPhone).IsRequired().HasMaxLength(20);
                entity.Property(cm => cm.Status).HasConversion<string>();
                entity.HasIndex(cm => cm.Status);
                entity.HasIndex(cm => cm.CreatedAt);
                entity.HasIndex(cm => cm.CampaignId);
                entity.HasIndex(cm => cm.VoterId);
            });

            // Configure Campaign relationships
            builder.Entity<CampaignMessage>()
                .HasOne(cm => cm.Campaign)
                .WithMany(c => c.Messages)
                .HasForeignKey(cm => cm.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CampaignMessage>()
                .HasOne(cm => cm.Voter)
                .WithMany()
                .HasForeignKey(cm => cm.VoterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure VolunteerResource entity
            builder.Entity<VolunteerResource>(entity =>
            {
                entity.HasKey(vr => vr.Id);
                entity.Property(vr => vr.ResourceType).IsRequired().HasMaxLength(50);
                entity.Property(vr => vr.Content).IsRequired();
                entity.HasIndex(vr => vr.ResourceType).IsUnique();
            });

            // Configure VoterTag entity
            builder.Entity<VoterTag>(entity =>
            {
                entity.HasKey(vt => vt.Id);
                entity.Property(vt => vt.TagName).IsRequired().HasMaxLength(50);
                entity.HasIndex(vt => vt.TagName).IsUnique();
                entity.Property(vt => vt.Description).HasMaxLength(200);
                entity.Property(vt => vt.Color).HasMaxLength(7);
            });

            // Configure VoterTagAssignment entity
            builder.Entity<VoterTagAssignment>(entity =>
            {
                entity.HasKey(vta => new { vta.VoterId, vta.TagId });
                entity.HasIndex(vta => vta.TagId);
                entity.HasIndex(vta => vta.AssignedAt);
            });

            // Configure VoterTag relationships
            builder.Entity<VoterTag>()
                .HasOne(vt => vt.CreatedBy)
                .WithMany()
                .HasForeignKey(vt => vt.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure VoterTagAssignment relationships
            builder.Entity<VoterTagAssignment>()
                .HasOne(vta => vta.Voter)
                .WithMany(v => v.TagAssignments)
                .HasForeignKey(vta => vta.VoterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VoterTagAssignment>()
                .HasOne(vta => vta.Tag)
                .WithMany(t => t.VoterAssignments)
                .HasForeignKey(vta => vta.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VoterTagAssignment>()
                .HasOne(vta => vta.AssignedBy)
                .WithMany()
                .HasForeignKey(vta => vta.AssignedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ConsentRecord entity
            builder.Entity<ConsentRecord>(entity =>
            {
                entity.HasKey(cr => cr.Id);
                entity.Property(cr => cr.VoterId).IsRequired();
                entity.Property(cr => cr.Action).HasConversion<string>();
                entity.Property(cr => cr.Method).HasConversion<string>();
                entity.Property(cr => cr.Timestamp).IsRequired();
                entity.Property(cr => cr.Source).HasMaxLength(255);
                entity.Property(cr => cr.Details).HasMaxLength(500);
                entity.Property(cr => cr.RawMessage).HasMaxLength(1600);
                entity.Property(cr => cr.IpAddress).HasMaxLength(45);
                entity.Property(cr => cr.UserAgent).HasMaxLength(500);
                entity.Property(cr => cr.FormUrl).HasMaxLength(255);
                entity.Property(cr => cr.ConsentLanguage).HasMaxLength(1000);
                entity.HasIndex(cr => cr.VoterId);
                entity.HasIndex(cr => cr.Timestamp);
                entity.HasIndex(cr => cr.Action);
            });

            // Configure ConsentRecord relationships
            builder.Entity<ConsentRecord>()
                .HasOne(cr => cr.Voter)
                .WithMany(v => v.ConsentRecords)
                .HasForeignKey(cr => cr.VoterId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}