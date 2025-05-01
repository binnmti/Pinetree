using Microsoft.Playwright;

namespace Pinetree.UITests
{
    [TestClass]
    public class NavigationTests : PlaywrightTestBase
    {
        [TestMethod]
        public async Task ShouldNavigateToHomePage()
        {
            await Page.GotoAsync(TargetUrl);
            StringAssert.Contains(await Page.TitleAsync(), "Pinetree");
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

            await Page.GotoAsync($"{TargetUrl}/List");

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            StringAssert.Contains(Page.Url, "/List");
        }
    }
}
