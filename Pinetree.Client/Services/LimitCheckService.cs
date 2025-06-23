using Microsoft.JSInterop;
using Pinetree.Client.ViewModels;

namespace Pinetree.Client.Services;

public static class LimitCheckService
{
    private const int FreeMaxDepth = 3;
    private const int FreeMaxChildFileCount = 33;
    // TODO:At this point, I would be grateful if you could create a file full of files.
    //private const int FreeMaxFileCount = 333;
    public const int FreeMaxCharacter = 3333;

    private const int ProMaxDepth = 9;

    public static async Task<bool> CheckDepthAsync(this IJSRuntime jsRuntime, PinetreeView pineTree, bool isProfessional)
    {
        var currentDepth = pineTree.GetDepth() + 1;
        if (isProfessional)
        {
            if (currentDepth > ProMaxDepth)
            {
                await jsRuntime.AlertAsync($"You cannot add more than {ProMaxDepth} levels in the professional version.");
                return true;
            }
        }
        else
        {
            if (currentDepth > FreeMaxDepth)
            {
                await jsRuntime.AlertAsync($"You cannot add more than {FreeMaxDepth} levels in the free version.");
                return true;
            }
        }
        return false;
    }

    public static async Task<bool> CheckFileCountAsync(this IJSRuntime jsRuntime, PinetreeView pineTree, bool isProfessional)
    {
        var fileCount = pineTree.GetTotalFileCount() + 1;
        if (!isProfessional)
        {
            if (fileCount > FreeMaxChildFileCount)
            {
                await jsRuntime.AlertAsync($"You cannot add more than {FreeMaxChildFileCount} files in the free version.");
                return true;
            }
        }
        return false;
    }

    public static async Task<bool> CheckMarkdownContentCharacterAsync(this IJSRuntime jsRuntime, int characterCount, bool isProfessional)
    {
        if (!isProfessional)
        {
            if (characterCount >= FreeMaxCharacter)
            {
                await jsRuntime.AlertAsync($"You cannot write more than {FreeMaxCharacter} content characters in the free version.");
                return true;
            }
        }
        return false;
    }
}
