using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Model;

namespace Pinetree.Components.Services;

public class PineconeService
{
    private readonly ApplicationDbContext _dbContext;

    public PineconeService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> CreatePineconeAsync(string userId)
    {
        var baseTitle = "Untitled";
        var title = baseTitle;
        var counter = 1;
        while (await _dbContext.Pinecone.AnyAsync(p => p.UserId == userId && p.Title == title))
        {
            title = $"{baseTitle} {counter}";
            counter++;
        }
        var pinecone = new Pinecone { Title = title, Content = "", GroupId = 0, ParentId = null, UserId = userId, IsSandbox = true };
        await _dbContext.Pinecone.AddAsync(pinecone);
        await _dbContext.SaveChangesAsync();
        pinecone.GroupId = pinecone.Id;
        await _dbContext.SaveChangesAsync();

        return $"/{userId}/Edit/{pinecone.Id}";
    }
}