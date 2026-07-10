using CUCoreLib.Data;
using CUCoreLib.Registries;

namespace CUZoomPlus;

internal static class Settings
{
    private const float DefaultZoomMultiplier = 1f;

    internal static float ZoomMultiplier { get; private set; } = DefaultZoomMultiplier;

    internal static void Register()
    {
        ModOptionsRegistry.Register(
            ModOptionDefinition.Float(
                "cuzoomplus.zoom.multiplier",
                "View scale",
                "Multiplies the default view. Values below 1.0 zoom in; values above 1.0 zoom out.",
                Setting.SettingCategory.Video,
                DefaultZoomMultiplier,
                0.25f,
                8f,
                value => ZoomMultiplier = value,
                value => $"{value:0.00}x"
            )
        );
    }
}
