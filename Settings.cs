using System.Reflection;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using UnityEngine;

namespace CUZoomPlus;

internal static class Settings
{
    private const string ZoomMultiplierSettingId = "cuzoomplus.zoom.multiplier";
    private const float DefaultZoomMultiplier = 1f;
    private const float DefaultZoomFactor = 0.5f;

    private static readonly MethodInfo SaveSettingsMethod = typeof(global::Settings).GetMethod(
        "SaveSettings",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
    );

    internal const float MinZoomMultiplier = 0.25f;
    internal const float MaxZoomMultiplier = 8f;

    internal static float ZoomMultiplier { get; private set; } = DefaultZoomMultiplier;
    internal static KeyCode ZoomInKey { get; private set; } = KeyCode.Equals;
    internal static KeyCode ZoomOutKey { get; private set; } = KeyCode.Minus;
    internal static float ZoomFactor { get; private set; } = DefaultZoomFactor;
    internal static bool SmoothZoom { get; private set; } = true;

    internal static void Register()
    {
        ModOptionsRegistry.Register(
            ModOptionDefinition.Float(
                ZoomMultiplierSettingId,
                "View scale",
                "The current view scale. It is also updated when the zoom keys are pressed.",
                Setting.SettingCategory.Video,
                DefaultZoomMultiplier,
                MinZoomMultiplier,
                MaxZoomMultiplier,
                value => ZoomMultiplier = value,
                FormatMultiplier
            )
        );

        ModOptionsRegistry.Register(
            ModOptionDefinition.Keybind(
                "cuzoomplus.zoom.in.key",
                "Zoom in",
                "Key used to zoom in. The default = key produces + while Shift is held.",
                Setting.SettingCategory.Input,
                KeyCode.Equals,
                value => ZoomInKey = value
            )
        );

        ModOptionsRegistry.Register(
            ModOptionDefinition.Keybind(
                "cuzoomplus.zoom.out.key",
                "Zoom out",
                "Key used to zoom out.",
                Setting.SettingCategory.Input,
                KeyCode.Minus,
                value => ZoomOutKey = value
            )
        );

        ModOptionsRegistry.Register(
            ModOptionDefinition.Float(
                "cuzoomplus.zoom.factor",
                "Zoom factor",
                "Zooming in multiplies the view scale by this value; zooming out divides by it.",
                Setting.SettingCategory.Video,
                DefaultZoomFactor,
                0.1f,
                1f,
                value => ZoomFactor = value,
                FormatMultiplier
            )
        );

        ModOptionsRegistry.Register(
            ModOptionDefinition.Bool(
                "cuzoomplus.zoom.smooth",
                "Smooth zoom",
                "Animates view scale changes instead of applying them immediately.",
                Setting.SettingCategory.Video,
                true,
                value => SmoothZoom = value
            )
        );
    }

    internal static void SetZoomMultiplier(float value)
    {
        value = Mathf.Clamp(value, MinZoomMultiplier, MaxZoomMultiplier);
        ZoomMultiplier = value;

        SettingFloat zoomSetting = null;
        if (global::Settings.settings == null)
        {
            return;
        }

        foreach (Setting setting in global::Settings.settings)
        {
            if (setting.name == ZoomMultiplierSettingId)
            {
                zoomSetting = setting as SettingFloat;
                break;
            }
        }

        if (zoomSetting == null)
        {
            return;
        }

        zoomSetting.value = value;
        zoomSetting.apply?.Invoke();
        SaveSettingsMethod?.Invoke(null, null);
    }

    private static string FormatMultiplier(float value) => $"{value:0.00}x";
}
