using Microsoft.JSInterop;
using Pinetree.Client.VModel;

namespace Pinetree.Client.Services;

public static class LimitCheckService
{
    private const int FreeMaxDepth = 3;
    private const int FreeMaxFileCount = 33;
    private const int FreeMaxCharacter = 33333;
    private const int ProMaxDepth = 9;
    private const int ProMaxFileCount = 99;
    private const int ProMaxCharacter = 99999;

    public static  int GetMaxCharacter(bool isProfessional)
    {
        if (isProfessional)
        {
            return ProMaxCharacter;
        }
        else
        {
            return FreeMaxCharacter;
        }
    }

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
        if (isProfessional)
        {
            if (fileCount >= ProMaxFileCount)
            {
                await jsRuntime.AlertAsync($"You cannot add more than {ProMaxFileCount} files in the professional version.");
                return true;
            }
        }
        else
        {
            if (fileCount > FreeMaxFileCount)
            {
                await jsRuntime.AlertAsync($"You cannot add more than {FreeMaxFileCount} files in the free version.");
                return true;
            }
        }
        return false;
    }

    public static async Task<bool> CheckCharacterAsync(this IJSRuntime jsRuntime, string character, bool isProfessional)
    {
        if (isProfessional)
        {
            if (character.Length >= ProMaxCharacter)
            {
                await jsRuntime.AlertAsync($"You cannot write more than {ProMaxCharacter} characters in the professional version.");
                return true;
            }
        }
        else
        {
            if (character.Length >= FreeMaxCharacter)
            {
                await jsRuntime.AlertAsync($"You cannot write more than {FreeMaxCharacter} characters in the free version.");
                return true;
            }
        }
        return false;
    }
}
