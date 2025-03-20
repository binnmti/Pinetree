using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using PinetreeModel;

namespace Pinetree.Components.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PineconesController : ControllerBase
    {
        private const string Untitled = "Untitled";
        private ApplicationDbContext DbContext { get; }

        public PineconesController(ApplicationDbContext context)
        {
            DbContext = context;
        }

        [HttpGet("add-top/{userName}")]
        public async Task<Pinecone> AddTop(string userName)
        {
            var title = Untitled;
            var counter = 1;
            while (await DbContext.Pinecone.AnyAsync(p => p.UserName == userName && p.Title == title && p.ParentId == null))
            {
                title = $"{Untitled} {counter}";
                counter++;
            }
            var pinecone = new Pinecone
            {
                Title = title,
                Content = "",
                GroupId = 0,
                ParentId = null,
                UserName = userName,
                IsDelete = false,
                Create = DateTime.UtcNow,
                Update = DateTime.UtcNow,
                Delete = DateTime.UtcNow,
            };
            await DbContext.Pinecone.AddAsync(pinecone);
            await DbContext.SaveChangesAsync();
            pinecone.GroupId = pinecone.Id;
            await DbContext.SaveChangesAsync();
            return pinecone;
        }

        [HttpGet("get-include-child/{id}")]
        public async Task<Pinecone> GetIncludeChild(long id)
        {
            var parent = await DbContext.Pinecone.SingleAsync(x => x.Id == id);
            return await LoadChildrenRecursivelyAsync(parent);
        }

        [HttpGet("get-user-top-list")]
        public async Task<List<Pinecone>> GetUserTopList([FromQuery] string userName, [FromQuery] int pageNumber, [FromQuery] int pageSize)
            => await GetUserTopList(userName).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        [HttpGet("get-user-top-count/{userName}")]
        public async Task<int> GetUserTopCount(string userName)
            => await GetUserTopList(userName).CountAsync();

        private IQueryable<Pinecone> GetUserTopList(string userName)
            => DbContext.Pinecone.Where(x => x.UserName == userName)
                .Where(x => x.ParentId == null)
                .Where(x => x.IsDelete == false);

        private async Task<Pinecone> LoadChildrenRecursivelyAsync(Pinecone parent)
        {
            await DbContext.Entry(parent).Collection(p => p.Children).Query().Where(x => x.IsDelete == false).LoadAsync();
            foreach (var child in parent.Children.Where(x => x.IsDelete == false))
            {
                await LoadChildrenRecursivelyAsync(child);
            }
            return parent;
        }
    }
}
