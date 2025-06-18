using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Pinetree.Client.Services;

namespace Pinetree.TestProject
{
    [TestClass]
    public class FontSettingsServiceTests
    {
        [TestMethod]
        public void GetAvailableFontFamilies_ShouldReturnValidFontList()
        {
            // Arrange & Act
            var fonts = FontSettingsService.GetAvailableFontFamilies();

            // Assert
            Assert.IsNotNull(fonts);
            Assert.IsTrue(fonts.Length > 0);
            Assert.IsTrue(fonts.Contains("system-ui, -apple-system, sans-serif"));
        }

        [TestMethod]
        public void GetFontDisplayName_ShouldReturnCorrectDisplayNames()
        {
            // Arrange & Act
            var systemFontName = FontSettingsService.GetFontDisplayName("system-ui, -apple-system, sans-serif");
            var georgiaFontName = FontSettingsService.GetFontDisplayName("Georgia, serif");
            var unknownFontName = FontSettingsService.GetFontDisplayName("Unknown Font");

            // Assert
            Assert.AreEqual("System Default", systemFontName);
            Assert.AreEqual("Georgia", georgiaFontName);
            Assert.AreEqual("System Default", unknownFontName);
        }
    }
}