using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pinetree.Shared.Model;

namespace Pinetree.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Pinecone>(entity =>
            {
                entity.HasOne(d => d.Parent)
                      .WithMany(p => p.Children)
                      .HasForeignKey(d => d.ParentGuid)
                      .HasPrincipalKey(p => p.Guid)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public DbSet<Pinecone> Pinecone { get; set; }
        public DbSet<UserBlobInfo> UserBlobInfos { get; set; }
        public DbSet<UserStorageUsage> UserStorageUsages { get; set; }
    }
}
