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
        var pinecone = new Pinecone { Title = "Untitled", Content = "", GroupId = 0, ParentId = null, UserId = userId, IsSandbox = true };
        await _dbContext.Pinecone.AddAsync(pinecone);
        await _dbContext.SaveChangesAsync();
        pinecone.GroupId = pinecone.Id;
        await _dbContext.SaveChangesAsync();

        return $"/markdown-editor/{userId}/{pinecone.Id}";
    }
}