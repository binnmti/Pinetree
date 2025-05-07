using Microsoft.Playwright;

namespace Pinetree.UITests
{
    [TestClass]
    public class NavigationTests : PlaywrightTestBase
    {
        [TestMethod]
        public async Task ShouldNavigateToTryitAndAddChildElements()
        {
            await Page.GotoAsync(TargetUrl);
            StringAssert.Contains(await Page.TitleAsync(), "Pinetree");

            await Page.ClickAsync("a[href='/Tryit']");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(1000);

            StringAssert.Contains(await Page.TitleAsync(), "Tryit");

            var initialFileCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(initialFileCount.Contains("FileCount : 1"));

            await Page.ClickAsync("button[title='Add Child Item']");
            await Page.WaitForTimeoutAsync(1000);

            var updatedFileCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(updatedFileCount.Contains("FileCount : 2"));

            await Page.ClickAsync("button[title='Add Child Item']");
            await Page.WaitForTimeoutAsync(1000);

            var finalFileCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(finalFileCount.Contains("FileCount : 3"));

            var childItems = await Page.QuerySelectorAllAsync("ul li ul li .title");
            if (childItems.Count > 0)
            {
                await childItems[0].ClickAsync();
            }

            Page.Dialog += (_, dialog) =>
            {
                if (dialog.Type == "confirm" &&
                    dialog.Message.Contains("Are you sure you want to delete"))
                {
                    dialog.AcceptAsync();
                }
            };

            await Page.ClickAsync("ul li ul li button[title='Delete Item']");
            await Page.WaitForTimeoutAsync(1000);

            var afterDeletionCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(afterDeletionCount.Contains("FileCount : 2"));
        }

        [TestMethod]
        public async Task ShouldLoginSuccessfully()
        {
            await Page.GotoAsync(TargetUrl);

            await Page.ClickAsync("a[href='Account/Login']");

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            StringAssert.Contains(Page.Url, "/Account/Login");

            await Page.FillAsync("input[id='Input.UserName']", "test");
            await Page.FillAsync("input[id='Input.Password']", "test");

            await Page.ClickAsync("button[type='submit']");

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Page.GotoAsync($"{TargetUrl}/User");

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            StringAssert.Contains(Page.Url, "/User");
        }
    }
}
