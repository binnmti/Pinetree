using Microsoft.JSInterop;
using Pinetree.Model;
namespace Pinetree.Components.Services;

public static class LimitCheckService
{
    private const int MaxDepth = 3;
    private const int MaxFileCount = 99;
    private const int MaxCharacter = 9999;

    public static async Task<bool> CheckDepthAsync(this IJSRuntime jsRuntime, Model.Pinetree pineTree)
    {
        var currentDepth = pineTree.GetDepth() + 1;
        if (currentDepth > MaxDepth)
        {
            await jsRuntime.InvokeVoidAsync("alert", $"You cannot add more than {MaxDepth} levels in the free version.");
            return true;
        }
        return false;
    }

    public static async Task<bool> CheckFileCountAsync(this IJSRuntime jsRuntime, Model.Pinetree pineTree)
    {
        var fileCount = pineTree.GetTotalFileCount() + 1;
        if (fileCount >= MaxFileCount)
        {
            await jsRuntime.InvokeVoidAsync("alert", $"You cannot add more than {MaxFileCount} files in the free version.");
            return true;
        }
        return false;
    }

    public static async Task<bool> CheckCharacterAsync(this IJSRuntime jsRuntime, string character)
    {
        if (character.Length >= MaxCharacter)
        {
            await jsRuntime.InvokeVoidAsync("alert", $"You cannot write more than {MaxCharacter} characters in the free version.");
            return true;
        }
        return false;
    }
}
