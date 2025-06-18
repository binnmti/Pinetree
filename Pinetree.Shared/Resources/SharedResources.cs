using System.Resources;

namespace Pinetree.Shared.Resources;

public class SharedResources
{
    private static readonly ResourceManager _resourceManager = new ResourceManager(typeof(SharedResources));

    public static ResourceManager ResourceManager => _resourceManager;
    
    // This class is intentionally empty as it serves as a marker class
    // for the resource files (SharedResources.resx and SharedResources.ja.resx)
}