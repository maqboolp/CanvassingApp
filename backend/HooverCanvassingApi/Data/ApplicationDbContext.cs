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
        public DbSet<VoiceRecording> VoiceRecordings { get; set; }
        public DbSet<AdditionalResource> AdditionalResources { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        
        // Walk feature tables
        public DbSet<WalkSession> WalkSessions { get; set; }
        public DbSet<HouseClaim> HouseClaims { get; set; }
        public DbSet<WalkActivity> WalkActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Voter entity
            builder.Entity<Voter>(entity =>
            {
                entity.ToTable("Voters"); // Explicitly set table name
                entity.HasKey(v => v.LalVoterId);
                entity.Property(v => v.LalVoterId).HasColumnName("lalvoterid");
                entity.Property(v => v.FirstName).HasColumnName("firstname").IsRequired().HasMaxLength(100);
                entity.Property(v => v.LastName).HasColumnName("lastname").IsRequired().HasMaxLength(100);
                entity.Property(v => v.MiddleName).HasColumnName("middlename");
                entity.Property(v => v.AddressLine).HasColumnName("addressline").IsRequired().HasMaxLength(500);
                entity.Property(v => v.City).HasColumnName("city").IsRequired().HasMaxLength(100);
                entity.Property(v => v.State).HasColumnName("state").IsRequired().HasMaxLength(50);
                entity.Property(v => v.Zip).HasColumnName("zip").IsRequired().HasMaxLength(10);
                entity.Property(v => v.Age).HasColumnName("age");
                entity.Property(v => v.Ethnicity).HasColumnName("ethnicity");
                entity.Property(v => v.Gender).HasColumnName("gender").IsRequired().HasMaxLength(20);
                entity.Property(v => v.VoteFrequency).HasColumnName("votefrequency").HasConversion<string>();
                entity.Property(v => v.CellPhone).HasColumnName("cellphone");
                entity.Property(v => v.Email).HasColumnName("email");
                entity.Property(v => v.Latitude).HasColumnName("latitude");
                entity.Property(v => v.Longitude).HasColumnName("longitude");
                entity.Property(v => v.IsContacted).HasColumnName("iscontacted");
                entity.Property(v => v.LastContactStatus).HasColumnName("lastcontactstatus").HasConversion<string>();
                entity.Property(v => v.LastCallAt).HasColumnName("lastcallat");
                entity.Property(v => v.CallCount).HasColumnName("callcount");
                entity.Property(v => v.LastSmsAt).HasColumnName("lastsmsat");
                entity.Property(v => v.SmsCount).HasColumnName("smscount");
                entity.Property(v => v.VoterSupport).HasColumnName("votersupport").HasConversion<string>();
                entity.Property(v => v.PartyAffiliation).HasColumnName("partyaffiliation");
                // Religion and Income columns don't exist in the database
                entity.Ignore(v => v.Religion);
                entity.Ignore(v => v.Income);
                entity.Property(v => v.LastCampaignId).HasColumnName("lastcampaignid");
                entity.Property(v => v.LastCallCampaignId).HasColumnName("lastcallcampaignid");
                entity.Property(v => v.LastSmsCampaignId).HasColumnName("lastsmscampaignid");
                entity.Property(v => v.LastCampaignContactAt).HasColumnName("lastcampaigncontactat");
                entity.Property(v => v.TotalCampaignContacts).HasColumnName("totalcampaigncontacts");
                entity.Property(v => v.SmsConsentStatus).HasColumnName("smsconsentstatus").HasConversion<string>();
                entity.Property(v => v.SmsOptInAt).HasColumnName("smsoptinat");
                entity.Property(v => v.SmsOptOutAt).HasColumnName("smsoptoutat");
                entity.Property(v => v.SmsOptInSource).HasColumnName("smsoptinsource");
                entity.Property(v => v.SmsOptInMethod).HasColumnName("smsoptinmethod").HasConversion<string>();
                entity.HasIndex(v => v.Zip);
                entity.HasIndex(v => v.VoteFrequency);
                entity.HasIndex(v => v.IsContacted);
                entity.HasIndex(v => v.SmsConsentStatus);
            });

            // Configure Volunteer entity
            builder.Entity<Volunteer>(entity =>
            {
                // AspNetUsers table has PascalCase columns, no need to map
                entity.Property(v => v.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(v => v.LastName).IsRequired().HasMaxLength(100);
                entity.Property(v => v.Role).HasConversion<string>();
            });

            // Configure Contact entity
            builder.Entity<Contact>(entity =>
            {
                // Contacts table has PascalCase columns, no need to map
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Status).HasConversion<string>();
                entity.Property(c => c.VoterSupport).HasConversion<string>();
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

            // Configure AdditionalResource entity
            builder.Entity<AdditionalResource>(entity =>
            {
                entity.HasKey(ar => ar.Id);
                entity.Property(ar => ar.Title).IsRequired().HasMaxLength(200);
                entity.Property(ar => ar.Url).IsRequired().HasMaxLength(500);
                entity.Property(ar => ar.Description).HasMaxLength(500);
                entity.Property(ar => ar.Category).HasMaxLength(100);
                entity.Property(ar => ar.CreatedBy).HasMaxLength(100);
                entity.Property(ar => ar.UpdatedBy).HasMaxLength(100);
                entity.HasIndex(ar => ar.Category);
                entity.HasIndex(ar => ar.IsActive);
                entity.HasIndex(ar => ar.DisplayOrder);
            });

            // Configure AppSetting entity
            builder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(app => app.Id);
                entity.Property(app => app.Key).IsRequired().HasMaxLength(100);
                entity.Property(app => app.Value).IsRequired();
                entity.Property(app => app.Description).HasMaxLength(500);
                entity.Property(app => app.Category).HasMaxLength(100);
                entity.Property(app => app.UpdatedBy).HasMaxLength(100);
                entity.HasIndex(app => app.Key).IsUnique();
                entity.HasIndex(app => app.Category);
                entity.HasIndex(app => app.IsPublic);
            });

            // Configure Walk feature entities
            builder.Entity<WalkSession>(entity =>
            {
                entity.HasKey(ws => ws.Id);
                entity.Property(ws => ws.Status).HasConversion<string>();
                entity.HasIndex(ws => ws.VolunteerId);
                entity.HasIndex(ws => ws.Status);
                entity.HasIndex(ws => ws.StartedAt);
            });

            builder.Entity<HouseClaim>(entity =>
            {
                entity.HasKey(hc => hc.Id);
                entity.Property(hc => hc.Address).IsRequired().HasMaxLength(200);
                entity.Property(hc => hc.Status).HasConversion<string>();
                entity.HasIndex(hc => hc.WalkSessionId);
                entity.HasIndex(hc => hc.Status);
                entity.HasIndex(hc => hc.Address);
                entity.HasIndex(hc => hc.ExpiresAt);
                entity.HasIndex(hc => new { hc.Latitude, hc.Longitude });
            });

            builder.Entity<WalkActivity>(entity =>
            {
                entity.HasKey(wa => wa.Id);
                entity.Property(wa => wa.ActivityType).HasConversion<string>();
                entity.Property(wa => wa.Description).HasMaxLength(500);
                entity.HasIndex(wa => wa.WalkSessionId);
                entity.HasIndex(wa => wa.Timestamp);
                entity.HasIndex(wa => wa.ActivityType);
            });

            // Configure Walk feature relationships
            builder.Entity<WalkSession>()
                .HasOne(ws => ws.Volunteer)
                .WithMany()
                .HasForeignKey(ws => ws.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<HouseClaim>()
                .HasOne(hc => hc.WalkSession)
                .WithMany(ws => ws.HouseClaims)
                .HasForeignKey(hc => hc.WalkSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WalkActivity>()
                .HasOne(wa => wa.WalkSession)
                .WithMany(ws => ws.Activities)
                .HasForeignKey(wa => wa.WalkSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WalkActivity>()
                .HasOne(wa => wa.HouseClaim)
                .WithMany()
                .HasForeignKey(wa => wa.HouseClaimId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}