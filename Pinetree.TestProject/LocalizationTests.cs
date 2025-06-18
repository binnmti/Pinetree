using Microsoft.Extensions.Localization;
using Pinetree.Shared.Resources;
using System.Globalization;

namespace Pinetree.TestProject;

[TestClass]
public class LocalizationTests
{
    [TestMethod]
    public void TestJapaneseLocalization()
    {
        // This is a simple test to ensure the localization strings exist
        // In a real scenario, you'd set up proper DI container and test the actual localization
        
        // For now, just test that the resource files exist and can be accessed
        var resourceManager = SharedResources.ResourceManager;
        
        // Test English (default)
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        var englishTitle = resourceManager.GetString("SiteTitle");
        Assert.AreEqual("Pinetree(Beta)", englishTitle);
        
        // Test Japanese
        CultureInfo.CurrentUICulture = new CultureInfo("ja");
        var japaneseTitle = resourceManager.GetString("SiteTitle");
        Assert.AreEqual("Pinetree(ベータ版)", japaneseTitle);
    }
    
    [TestMethod]
    public void TestAllResourceKeysExist()
    {
        var resourceManager = SharedResources.ResourceManager;
        
        var keys = new[]
        {
            "SiteTitle",
            "SiteDescription", 
            "MainHeading",
            "Hierarchical",
            "Playground",
            "Register"
        };
        
        foreach (var key in keys)
        {
            // Test English
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var englishValue = resourceManager.GetString(key);
            Assert.IsNotNull(englishValue);
            Assert.IsTrue(englishValue.Length > 0);
            
            // Test Japanese  
            CultureInfo.CurrentUICulture = new CultureInfo("ja");
            var japaneseValue = resourceManager.GetString(key);
            Assert.IsNotNull(japaneseValue);
            Assert.IsTrue(japaneseValue.Length > 0);
        }
    }
}