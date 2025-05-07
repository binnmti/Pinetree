using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Playwright;

namespace Pinetree.UITests
{
    [TestClass]
    public class PlaywrightTestBase
    {
        protected string TargetUrl { get; private set; }
        protected IBrowser Browser { get; private set; } = null!;
        protected IBrowserContext Context { get; private set; } = null!;
        protected IPage Page { get; private set; } = null!;
        private IPlaywright Playwright { get; set; } = null!;
        protected IHostEnvironment Environment { get; private set; } = null!;
        public TestContext TestContext { get; set; } = null!;

        public PlaywrightTestBase()
        {
            var environmentName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            Environment = new HostingEnvironment
            {
                EnvironmentName = environmentName
            };

            TargetUrl = DetermineTargetUrl();
            Directory.CreateDirectory("TestResults/Videos");
            Directory.CreateDirectory("TestResults/Traces");
            Directory.CreateDirectory("TestResults/Screenshots");
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
            else
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
                Headless = false,
                //Headless = !Environment.IsDevelopment(),
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                RecordVideoDir = "TestResults/Videos"
            });

            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            Page = await Context.NewPageAsync();

            await Page.GotoAsync(TargetUrl);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            string testName = TestContext.TestName ?? "UnknownTest";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            Directory.CreateDirectory("TestResults/Traces");
            Directory.CreateDirectory("TestResults/Screenshots");

            var testOutcome = TestContext.CurrentTestOutcome;
            if (testOutcome != UnitTestOutcome.Passed && Page != null)
            {
                try
                {
                    string screenshotPath = Path.Combine("TestResults/Screenshots", $"{testName}_{timestamp}.png");
                    await Page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = screenshotPath,
                        FullPage = true
                    });
                    TestContext.AddResultFile(screenshotPath);
                    Console.WriteLine($"Screenshot saved to: {screenshotPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to take screenshot: {ex.Message}");
                }
            }

            try
            {
                await Context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = Path.Combine("TestResults/Traces", $"{testName}_{timestamp}.zip")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save trace: {ex.Message}");
            }

            await Context.CloseAsync();
            await Browser.CloseAsync();
            Playwright?.Dispose();
        }
    }
}
