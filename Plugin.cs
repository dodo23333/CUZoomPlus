using System.Collections;
using BepInEx;
using BepInEx.Logging;
using CUCoreLib.Helpers;
using UnityEngine;
using UnityEngine.Rendering;

namespace CUZoomPlus;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private Camera activeCamera;
    private Transform sightLimiter;
    private float normalOrthographicSize;
    private Vector3 normalSightLimiterScale;

    private void Awake()
    {
        Logger = base.Logger;

        Settings.Register();
        CUCoreUtils.StartCoroutine(ZoomLoop());

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private IEnumerator ZoomLoop()
    {
        while (true)
        {
            yield return null;
            UpdateZoom();
        }
    }

    private void UpdateZoom()
    {
        if (!CUCoreUtils.TryGetCamera(out PlayerCamera playerCamera))
        {
            ReleaseCamera();
            return;
        }

        Camera renderingCamera = playerCamera.GetComponent<Camera>();
        if (!renderingCamera)
        {
            renderingCamera = Camera.main;
        }

        if (!renderingCamera)
        {
            ReleaseCamera();
            return;
        }

        TrackCamera(renderingCamera);
        ApplyZoom(renderingCamera);
    }

    private void TrackCamera(Camera renderingCamera)
    {
        if (activeCamera == renderingCamera)
        {
            return;
        }

        ReleaseCamera();
        activeCamera = renderingCamera;
        sightLimiter = renderingCamera.transform.Find("SightLimiter");
        normalOrthographicSize = renderingCamera.orthographicSize;

        if (sightLimiter)
        {
            normalSightLimiterScale = sightLimiter.localScale;
        }

        RenderPipelineManager.beginCameraRendering += ApplyZoomBeforeRendering;
    }

    private void ApplyZoomBeforeRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        ApplyZoom(renderingCamera);
    }

    private void ApplyZoom(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera)
        {
            return;
        }

        renderingCamera.orthographicSize = normalOrthographicSize * Settings.ZoomMultiplier;

        if (sightLimiter)
        {
            sightLimiter.localScale = new Vector3(
                normalSightLimiterScale.x * Settings.ZoomMultiplier,
                normalSightLimiterScale.y * Settings.ZoomMultiplier,
                normalSightLimiterScale.z
            );
        }
    }

    private void ReleaseCamera()
    {
        RenderPipelineManager.beginCameraRendering -= ApplyZoomBeforeRendering;

        if (sightLimiter)
        {
            sightLimiter.localScale = normalSightLimiterScale;
        }

        if (activeCamera)
        {
            activeCamera.orthographicSize = normalOrthographicSize;
        }

        activeCamera = null;
        sightLimiter = null;
        normalOrthographicSize = 0f;
        normalSightLimiterScale = Vector3.one;
    }
}
