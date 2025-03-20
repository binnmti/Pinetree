﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using PinetreeModel;
using System.Diagnostics;

namespace Pinetree.Components.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PineconesController(ApplicationDbContext context) : ControllerBase
    {
        private const string Untitled = "Untitled";
        private ApplicationDbContext DbContext { get; } = context;

        [HttpPost("add-top")]
        public async Task<Pinecone> AddTop()
        {
            var userName = User.Identity?.Name ?? "";

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

        [HttpPost("add-child")]
        public async Task<Pinecone> AddChild([FromQuery] long groupId, [FromQuery] long parentId)
        {
            var userName = User.Identity?.Name ?? "";

            var pinecone = new Pinecone()
            {
                Title = Untitled,
                Content = "",
                GroupId = groupId,
                ParentId = parentId,
                UserName = userName,
                IsDelete = false,
                Create = DateTime.UtcNow,
                Update = DateTime.UtcNow,
                Delete = DateTime.UtcNow,
            };
            await DbContext.Pinecone.AddAsync(pinecone);
            await DbContext.SaveChangesAsync();
            return pinecone;
        }

        public record PineconeUpdateModel(long Id, string Title, string Content);
        [HttpPost("update")]
        public async Task Update([FromBody] PineconeUpdateModel model)
        {
            var current = await DbContext.Pinecone.SingleAsync(x => x.Id == model.Id);
            current.Title = model.Title;
            current.Content = model.Content;
            current.Update = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();
        }

        [HttpDelete("delete-include-child/{id}")]
        public async Task<Pinecone> DeleteIncludeChild(long id)
        {
            var pinecone = await SingleIncludeChild(id);
            Debug.Assert(pinecone != null);
            pinecone.Delete = DateTime.UtcNow;
            pinecone.IsDelete = true;
            DeleteChildren(pinecone);
            pinecone.Parent?.Children.Remove(pinecone);
            await DbContext.SaveChangesAsync();
            return pinecone;
        }

        [HttpGet("get-include-child/{id}")]
        public async Task<Pinecone> GetIncludeChild(long id)
        {
            var parent = await DbContext.Pinecone.SingleAsync(x => x.Id == id);
            return await LoadChildrenRecursivel(parent);
        }

        [HttpGet("get-user-top-list")]
        public async Task<List<Pinecone>> GetUserTopList([FromQuery] int pageNumber, [FromQuery] int pageSize)
        {
            var userName = User.Identity?.Name ?? "";
            return await GetUserTopList(userName).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        [HttpGet("get-user-top-count")]
        public async Task<int> GetUserTopCount()
        {
            var userName = User.Identity?.Name ?? "";
            return await GetUserTopList(userName).CountAsync();
        }

        public async Task<Pinecone> SingleIncludeChild(long id)
        {
            var parent = await DbContext.Pinecone.SingleAsync(x => x.Id == id);
            return await LoadChildrenRecursivel(parent);
        }

        private static void DeleteChildren(Pinecone parent)
        {
            foreach (var child in parent.Children)
            {
                DeleteChildren(child);
                child.Delete = DateTime.UtcNow;
                child.IsDelete = true;
            }
            parent.Children.Clear();
        }

        private IQueryable<Pinecone> GetUserTopList(string userName)
            => DbContext.Pinecone.Where(x => x.UserName == userName)
                .Where(x => x.ParentId == null)
                .Where(x => x.IsDelete == false);

        private async Task<Pinecone> LoadChildrenRecursivel(Pinecone parent)
        {
            await DbContext.Entry(parent).Collection(p => p.Children).Query().Where(x => x.IsDelete == false).LoadAsync();
            foreach (var child in parent.Children.Where(x => x.IsDelete == false))
            {
                await LoadChildrenRecursivel(child);
            }
            return parent;
        }
    }
}
