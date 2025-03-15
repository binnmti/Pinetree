using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pinetree.Model;

namespace Pinetree.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Pinecone> Pinecone { get; set; }
        public DbSet<TryUser> TryUser { get; set; }
    }
}
