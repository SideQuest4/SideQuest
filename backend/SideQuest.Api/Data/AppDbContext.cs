using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Models;

namespace SideQuest.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<QuestSlot> QuestSlots => Set<QuestSlot>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<EscrowPayment> EscrowPayments => Set<EscrowPayment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Auth0Id).IsUnique();
        });

        b.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
        });

        b.Entity<Quest>(e =>
        {
            e.HasIndex(q => q.Status);
            e.HasIndex(q => q.CreatedAt);

            e.HasOne(q => q.Poster)
                .WithMany(u => u.PostedQuests)
                .HasForeignKey(q => q.PosterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(q => q.Category)
                .WithMany(c => c.Quests)
                .HasForeignKey(q => q.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // These are computed in code, not stored.
            e.Ignore(q => q.SlotCount);
            e.Ignore(q => q.OpenSlotCount);
            e.Ignore(q => q.IsMultiSlot);
        });

        b.Entity<QuestSlot>(e =>
        {
            e.HasOne(s => s.Quest)
                .WithMany(q => q.Slots)
                .HasForeignKey(s => s.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.AssignedQuester)
                .WithMany()
                .HasForeignKey(s => s.AssignedQuesterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.AcceptedBid)
                .WithMany()
                .HasForeignKey(s => s.AcceptedBidId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Bid>(e =>
        {
            e.HasIndex(x => new { x.QuestId, x.QuesterId });
            e.Ignore(x => x.EffectiveAmountCents);

            e.HasOne(x => x.Quest)
                .WithMany(q => q.Bids)
                .HasForeignKey(x => x.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Quester)
                .WithMany(u => u.Bids)
                .HasForeignKey(x => x.QuesterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<EscrowPayment>(e =>
        {
            e.HasIndex(p => p.Status);
            e.Ignore(p => p.PosterChargeCents);
            e.Ignore(p => p.QuesterPayoutCents);

            e.HasOne(p => p.Quest)
                .WithMany(q => q.EscrowPayments)
                .HasForeignKey(p => p.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Slot)
                .WithMany()
                .HasForeignKey(p => p.SlotId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Bid)
                .WithMany()
                .HasForeignKey(p => p.BidId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Rating>(e =>
        {
            e.HasOne(r => r.Quest)
                .WithMany()
                .HasForeignKey(r => r.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Rater)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Ratee)
                .WithMany(u => u.RatingsReceived)
                .HasForeignKey(r => r.RateeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
