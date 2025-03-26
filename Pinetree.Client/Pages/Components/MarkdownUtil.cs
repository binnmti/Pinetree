using Microsoft.JSInterop;
using Pinetree.Client.Model;
using Pinetree.Client.Services;
using Pinetree.Shared.Model;
using System.Net.Http.Json;

namespace Pinetree.Client.Pages.Components;

internal static class MarkdownUtil
{
    internal static string GetChildTitle(string text)
    {
        var firstLine = text.Split('\n').FirstOrDefault()?.Trim() ?? "";
        firstLine = firstLine.TrimStart('#', ' ');
        if (firstLine.Length > 40)
        {
            return string.Concat(firstLine.AsSpan(0, 37), "...");
        }
        return string.IsNullOrEmpty(firstLine) ? "Extracted content" : firstLine;
    }

    internal static async Task<long> AddChildAsync(Model.Pinetree current, string title, string content, IJSRuntime js, HttpClient httpClient, bool isTry, bool isProfessional)
    {
        if (await js.CheckDepthAsync(current, isProfessional))
        {
            return -1;
        }
        if (await js.CheckFileCountAsync(current, isProfessional))
        {
            return -1;
        }

        Model.Pinetree pinetree;
        if (isTry)
        {
            var uniqueId = current.GetUniqueId();
            pinetree = new Model.Pinetree(uniqueId, title, content, current, -1);
        }
        else
        {
            var child = new
            {
                Id = -1,
                title,
                content,
                current.GroupId,
                ParentId = current.Id,
            };
            var response = await httpClient.PostAsJsonAsync($"/api/Pinecones/add-child", child);
            if (!response.IsSuccessStatusCode) return -1;
            var pinecone = await response.Content.ReadFromJsonAsync<Pinecone>();
            if (pinecone == null) return -1;

            pinetree = pinecone.ToPinetree(current);
        }
        // If you don't save it, the ID won't be decided.
        current.IsExpanded = true;
        current.Children.Add(pinetree);
        return pinetree.Id;
    }
}
