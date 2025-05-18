using Microsoft.JSInterop;
using Pinetree.Client.VModel;
using Pinetree.Client.Services;

namespace Pinetree.Client.Pages.Components;

internal static class MarkdownUtil
{
    internal static string GetChildTitle(string text)
    {
        var firstLine = text.Split('\n').FirstOrDefault()?.Trim() ?? "";
        firstLine = firstLine.TrimStart('#', '-', '*', '>', '`', '+', '~', '[', ']', '(', ')', '!', ':', '|', ' ');
        if (firstLine.StartsWith("[ ]") || firstLine.StartsWith("[x]"))
        {
            firstLine = firstLine[3..].TrimStart();
        }
        if (firstLine.Length > 40)
        {
            return string.Concat(firstLine.AsSpan(0, 37), "...");
        }
        return string.IsNullOrEmpty(firstLine) ? "Untitled" : firstLine;
    }


    internal static async Task<Guid> AddChildAsync(PinetreeView current, string title, string content, IJSRuntime js, HttpClient httpClient, bool isTry, bool isProfessional)
    {
        if (await js.CheckDepthAsync(current, isProfessional))
        {
            return default;
        }
        if (await js.CheckFileCountAsync(current, isProfessional))
        {
            return default;
        }

        PinetreeView pinetree;
        var uniqueId = current.GetUniqueId();
        pinetree = new PinetreeView(uniqueId, title, content, current, default, false);
        // If you don't save it, the ID won't be decided.
        current.IsExpanded = true;
        current.Children.Add(pinetree);
        return pinetree.Guid;
    }
}
