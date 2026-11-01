using Harjoitustyö.Models;
using Microsoft.EntityFrameworkCore;

namespace Harjoitustyö.Data
{
    public class MessageServiceContext : DbContext
    {
        public MessageServiceContext(DbContextOptions<MessageServiceContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.Property(u => u.UserName).HasMaxLength(50).IsRequired();
                entity.Property(u => u.FirstName).HasMaxLength(50);
                entity.Property(u => u.LastName).HasMaxLength(50);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(m => m.Title).HasMaxLength(100).IsRequired();
                entity.Property(m => m.Body).HasMaxLength(2000).IsRequired();

                // Poistot hoidetaan repositoriossa itse, jotta ketjut eivät katkea hallitsemattomasti
                entity.HasOne(m => m.Sender).WithMany()
                    .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.Recipient).WithMany()
                    .HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.PreviousMessage).WithMany()
                    .HasForeignKey(m => m.PreviousMessageId).OnDelete(DeleteBehavior.ClientSetNull);
            });
        }
    }
}
