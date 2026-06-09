using ChatApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<ChatGroup> Groups => Set<ChatGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ConversationRead> ConversationReads => Set<ConversationRead>();
    public DbSet<UserMessageHide> UserMessageHides => Set<UserMessageHide>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Admin>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Friendship>(e =>
        {
            e.HasKey(x => new { x.UserId, x.FriendId });
            e.HasOne(x => x.User).WithMany(u => u.Friendships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Friend).WithMany().HasForeignKey(x => x.FriendId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FriendRequest>(e =>
        {
            e.HasOne(x => x.FromUser).WithMany(u => u.SentRequests).HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToUser).WithMany(u => u.ReceivedRequests).HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupMember>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.UserId });
        });

        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasOne(x => x.Group).WithOne(g => g.Conversation).HasForeignKey<Conversation>(x => x.GroupId);
            e.HasIndex(x => new { x.Type, x.UserAId, x.UserBId }).IsUnique();
        });

        modelBuilder.Entity<ConversationRead>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ConversationId });
        });

        modelBuilder.Entity<UserMessageHide>(e =>
        {
            e.HasKey(x => new { x.UserId, x.MessageId });
        });
    }
}
