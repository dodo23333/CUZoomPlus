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
    private const float SmoothZoomSpeed = 15f;

    internal static new ManualLogSource Logger;

    private Camera activeCamera;
    private Transform sightLimiter;
    private float normalOrthographicSize;
    private Vector3 normalSightLimiterScale;
    private float currentZoomMultiplier = 1f;
    private float targetZoomMultiplier = 1f;
    private float lastConfiguredZoomMultiplier = 1f;

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
        SyncConfiguredZoom();
        HandleZoomInput();
        UpdateZoomAnimation();
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
        currentZoomMultiplier = Settings.ZoomMultiplier;
        targetZoomMultiplier = Settings.ZoomMultiplier;
        lastConfiguredZoomMultiplier = Settings.ZoomMultiplier;

        if (sightLimiter)
        {
            normalSightLimiterScale = sightLimiter.localScale;
        }

        RenderPipelineManager.beginCameraRendering += ApplyZoomBeforeRendering;
    }

    private void SyncConfiguredZoom()
    {
        if (Mathf.Approximately(lastConfiguredZoomMultiplier, Settings.ZoomMultiplier))
        {
            return;
        }

        lastConfiguredZoomMultiplier = Settings.ZoomMultiplier;
        targetZoomMultiplier = Settings.ZoomMultiplier;
    }

    private void HandleZoomInput()
    {
        float requestedMultiplier = targetZoomMultiplier;

        if (Input.GetKeyDown(Settings.ZoomInKey))
        {
            requestedMultiplier *= Settings.ZoomFactor;
        }

        if (Input.GetKeyDown(Settings.ZoomOutKey))
        {
            requestedMultiplier /= Settings.ZoomFactor;
        }

        requestedMultiplier = Mathf.Clamp(
            requestedMultiplier,
            Settings.MinZoomMultiplier,
            Settings.MaxZoomMultiplier
        );

        if (Mathf.Approximately(requestedMultiplier, targetZoomMultiplier))
        {
            return;
        }

        targetZoomMultiplier = requestedMultiplier;
        lastConfiguredZoomMultiplier = requestedMultiplier;
        Settings.SetZoomMultiplier(requestedMultiplier);
    }

    private void UpdateZoomAnimation()
    {
        if (!Settings.SmoothZoom)
        {
            currentZoomMultiplier = targetZoomMultiplier;
            return;
        }

        float interpolation = 1f - Mathf.Exp(-SmoothZoomSpeed * Time.unscaledDeltaTime);
        currentZoomMultiplier = Mathf.Lerp(currentZoomMultiplier, targetZoomMultiplier, interpolation);

        if (Mathf.Abs(currentZoomMultiplier - targetZoomMultiplier) < 0.0001f)
        {
            currentZoomMultiplier = targetZoomMultiplier;
        }
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

        renderingCamera.orthographicSize = normalOrthographicSize * currentZoomMultiplier;

        if (sightLimiter)
        {
            sightLimiter.localScale = new Vector3(
                normalSightLimiterScale.x * currentZoomMultiplier,
                normalSightLimiterScale.y * currentZoomMultiplier,
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
        currentZoomMultiplier = Settings.ZoomMultiplier;
        targetZoomMultiplier = Settings.ZoomMultiplier;
        lastConfiguredZoomMultiplier = Settings.ZoomMultiplier;
    }
}
