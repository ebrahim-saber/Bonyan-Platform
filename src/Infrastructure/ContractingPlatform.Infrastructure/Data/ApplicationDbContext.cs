using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Domain.Common;
using ContractingPlatform.Domain.Entities;

namespace ContractingPlatform.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<ContractorProfile> ContractorProfiles => Set<ContractorProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<ContractorService> ContractorServices => Set<ContractorService>();
    public DbSet<ProjectRequest> ProjectRequests => Set<ProjectRequest>();
    public DbSet<ProjectAttachment> ProjectAttachments => Set<ProjectAttachment>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<ProjectContract> ProjectContracts => Set<ProjectContract>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<ProjectReview> ProjectReviews => Set<ProjectReview>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SecurityLog> SecurityLogs => Set<SecurityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser to ClientProfile (1 to 0..1)
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.ClientProfile)
            .WithOne(c => c.User)
            .HasForeignKey<ClientProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ApplicationUser to ContractorProfile (1 to 0..1)
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.ContractorProfile)
            .WithOne(c => c.User)
            .HasForeignKey<ContractorProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ClientProfile to ProjectRequest (1 to Many)
        builder.Entity<ProjectRequest>()
            .HasOne(p => p.Client)
            .WithMany(c => c.ProjectRequests)
            .HasForeignKey(p => p.ClientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category to ProjectRequest
        builder.Entity<ProjectRequest>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProjectRequest to Bid (1 to Many)
        builder.Entity<Bid>()
            .HasOne(b => b.ProjectRequest)
            .WithMany(p => p.Bids)
            .HasForeignKey(b => b.ProjectRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // ContractorProfile to Bid (1 to Many)
        builder.Entity<Bid>()
            .HasOne(b => b.Contractor)
            .WithMany(c => c.Bids)
            .HasForeignKey(b => b.ContractorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProjectContract Relationships
        builder.Entity<ProjectContract>()
            .HasOne(pc => pc.ProjectRequest)
            .WithOne(p => p.Contract)
            .HasForeignKey<ProjectContract>(pc => pc.ProjectRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectContract>()
            .HasOne(pc => pc.AcceptedBid)
            .WithOne(b => b.Contract)
            .HasForeignKey<ProjectContract>(pc => pc.AcceptedBidId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectContract>()
            .HasOne(pc => pc.Client)
            .WithMany(c => c.Contracts)
            .HasForeignKey(pc => pc.ClientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectContract>()
            .HasOne(pc => pc.Contractor)
            .WithMany(c => c.Contracts)
            .HasForeignKey(pc => pc.ContractorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Milestones
        builder.Entity<ProjectMilestone>()
            .HasOne(m => m.Contract)
            .WithMany(c => c.Milestones)
            .HasForeignKey(m => m.ProjectContractId)
            .OnDelete(DeleteBehavior.Cascade);

        // PaymentTransactions
        builder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Contract)
            .WithMany(c => c.Transactions)
            .HasForeignKey(pt => pt.ProjectContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Milestone)
            .WithOne(m => m.Transaction)
            .HasForeignKey<PaymentTransaction>(pt => pt.MilestoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Client)
            .WithMany(c => c.Transactions)
            .HasForeignKey(pt => pt.ClientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Contractor)
            .WithMany(c => c.Transactions)
            .HasForeignKey(pt => pt.ContractorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProjectReview
        builder.Entity<ProjectReview>()
            .HasOne(pr => pr.Contract)
            .WithOne(c => c.Review)
            .HasForeignKey<ProjectReview>(pr => pr.ProjectContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectReview>()
            .HasOne(pr => pr.Client)
            .WithMany(c => c.ReviewsGiven)
            .HasForeignKey(pr => pr.ClientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectReview>()
            .HasOne(pr => pr.Contractor)
            .WithMany(c => c.ReviewsReceived)
            .HasForeignKey(pr => pr.ContractorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // ChatMessages
        builder.Entity<ChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChatMessage>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precisions
        builder.Entity<ContractorProfile>()
            .Property(c => c.Rating)
            .HasPrecision(3, 2);

        builder.Entity<ProjectRequest>()
            .Property(p => p.ExpectedBudgetMin)
            .HasPrecision(18, 2);

        builder.Entity<ProjectRequest>()
            .Property(p => p.ExpectedBudgetMax)
            .HasPrecision(18, 2);

        builder.Entity<Bid>()
            .Property(b => b.ProposedPrice)
            .HasPrecision(18, 2);

        builder.Entity<Bid>()
            .Property(b => b.MaterialCost)
            .HasPrecision(18, 2);

        builder.Entity<Bid>()
            .Property(b => b.LaborCost)
            .HasPrecision(18, 2);

        builder.Entity<ProjectContract>()
            .Property(c => c.TotalAmount)
            .HasPrecision(18, 2);

        builder.Entity<ProjectContract>()
            .Property(c => c.PlatformCommissionPercentage)
            .HasPrecision(5, 2);

        builder.Entity<ProjectContract>()
            .Property(c => c.PlatformCommissionAmount)
            .HasPrecision(18, 2);

        builder.Entity<ProjectContract>()
            .Property(c => c.ContractorNetAmount)
            .HasPrecision(18, 2);

        builder.Entity<ProjectMilestone>()
            .Property(m => m.Amount)
            .HasPrecision(18, 2);

        builder.Entity<PaymentTransaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Entity<PaymentTransaction>()
            .Property(t => t.PlatformFee)
            .HasPrecision(18, 2);

        builder.Entity<PaymentTransaction>()
            .Property(t => t.NetAmount)
            .HasPrecision(18, 2);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
