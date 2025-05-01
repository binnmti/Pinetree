using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Playwright;

namespace Pinetree.UITests
{
    [TestClass]
    public class PlaywrightTestBase
    {
        protected string TargetUrl { get; private set; }
        protected IBrowser Browser { get; private set; }
        protected IBrowserContext Context { get; private set; }
        protected IPage Page { get; private set; }
        private IPlaywright Playwright { get; set; }
        protected IHostEnvironment Environment { get; private set; }

        public PlaywrightTestBase()
        {
            // ���ϐ�����������擾
            var environmentName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // ASP.NET Core�Ɠ����z�X�e�B���O�����쐬
            Environment = new HostingEnvironment
            {
                EnvironmentName = environmentName
            };

            // ���Ɋ�Â���URL��ݒ�
            TargetUrl = DetermineTargetUrl();
        }

        private string DetermineTargetUrl()
        {
            if (Environment.IsDevelopment())
            {
                return "https://localhost:7263";
            }
            else if (Environment.IsStaging())
            {
                return "https://pinetree-staging.azurewebsites.net";
            }
            else // Production
            {
                return "https://pinetree.azurewebsites.net";
            }
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !Environment.IsDevelopment(),
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
            });

            Page = await Context.NewPageAsync();

            await Page.GotoAsync(TargetUrl);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await Context?.CloseAsync();
            await Browser?.CloseAsync();
            Playwright?.Dispose();
        }
    }
}
