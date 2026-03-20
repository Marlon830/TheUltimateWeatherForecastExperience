using System;
using System.Collections.Generic;
using Tenkoku.Core;
using UnityEngine;

public class StormLightningController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject stormWeatherRoot;

    [Header("Lightning")]
    [SerializeField] private bool enableLightning = true;
    [SerializeField, Min(0.05f)] private float lightningMinInterval = 1.4f;
    [SerializeField, Min(0.05f)] private float lightningMaxInterval = 4f;
    [SerializeField, Min(0.01f)] private float lightningFlashDuration = 0.12f;
    [SerializeField, Min(2)] private int lightningBoltPoints = 18;
    [SerializeField] private float lightningVerticalLength = 8f;
    [SerializeField] private float lightningHorizontalJitter = 0.8f;
    [SerializeField] private float lightningRadiusAroundPlayer = 10f;
    [SerializeField, Range(0f, 1f)] private float lightningLineAlpha = 1f;

    [Header("Thunder (Optional)")]
    [SerializeField] private AudioSource thunderAudio;
    [SerializeField] private AudioClip[] thunderClips;
    [SerializeField, Range(0f, 1f)] private float thunderVolume = 1f;

    float nextLightningTime;
    float lightningHideTime;
    bool lightningReady;
    LineRenderer lightningRenderer;

    void OnEnable()
    {
        ConfigureLightning();
    }

    public void SetStormWeatherRoot(GameObject root)
    {
        stormWeatherRoot = root;
        ConfigureLightning();
    }

    void Update()
    {
        if (!lightningReady || lightningRenderer == null)
            return;

        bool stormActive = stormWeatherRoot != null && stormWeatherRoot.activeInHierarchy;
        if (!stormActive)
        {
            if (lightningRenderer.enabled)
                lightningRenderer.enabled = false;
            return;
        }

        if (lightningRenderer.enabled && Time.unscaledTime >= lightningHideTime)
            lightningRenderer.enabled = false;

        if (Time.unscaledTime >= nextLightningTime)
            TriggerLightningFlash();
    }

    void ConfigureLightning()
    {
        lightningReady = false;
        lightningRenderer = null;

        if (stormWeatherRoot == null)
            return;

        TenkokuLightningFX tenkokuLightning = stormWeatherRoot.GetComponentInChildren<TenkokuLightningFX>(true);
        if (tenkokuLightning != null)
        {
            bool hasTenkokuCore = FindObjectOfType<TenkokuModule>() != null && FindObjectOfType<TenkokuLib>() != null;
            if (!hasTenkokuCore)
                tenkokuLightning.enabled = false;

            if ((thunderClips == null || thunderClips.Length == 0))
                thunderClips = CollectThunderClipsFromTenkoku(tenkokuLightning);
        }

        if (!enableLightning)
            return;

        lightningRenderer = stormWeatherRoot.GetComponentInChildren<LineRenderer>(true);
        if (lightningRenderer == null)
            return;

        lightningRenderer.enabled = false;
        lightningRenderer.positionCount = Mathf.Max(lightningBoltPoints, 2);
        ScheduleNextLightningFlash();
        lightningReady = true;
    }

    void TriggerLightningFlash()
    {
        Vector3 origin = stormWeatherRoot != null ? stormWeatherRoot.transform.position : transform.position;
        if (Camera.main != null)
            origin = Camera.main.transform.position;

        float radius = Mathf.Max(1f, lightningRadiusAroundPlayer);
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float distance = UnityEngine.Random.Range(radius * 0.35f, radius);
        Vector3 strikeGround = new Vector3(
            origin.x + Mathf.Cos(angle) * distance,
            Mathf.Max(origin.y + 1f, (stormWeatherRoot != null ? stormWeatherRoot.transform.position.y : origin.y) - 2f),
            origin.z + Mathf.Sin(angle) * distance);

        Vector3 strikeTop = strikeGround + Vector3.up * Mathf.Max(1.5f, lightningVerticalLength);

        int pointCount = Mathf.Max(2, lightningBoltPoints);
        if (lightningRenderer.positionCount != pointCount)
            lightningRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (pointCount - 1f);
            Vector3 point = Vector3.Lerp(strikeTop, strikeGround, t);

            if (i > 0 && i < pointCount - 1)
            {
                point.x += UnityEngine.Random.Range(-lightningHorizontalJitter, lightningHorizontalJitter);
                point.z += UnityEngine.Random.Range(-lightningHorizontalJitter, lightningHorizontalJitter);
            }

            lightningRenderer.SetPosition(i, point);
        }

        Color startColor = lightningRenderer.startColor;
        Color endColor = lightningRenderer.endColor;
        float alpha = Mathf.Clamp01(lightningLineAlpha);
        startColor.a = alpha;
        endColor.a = alpha;
        lightningRenderer.startColor = startColor;
        lightningRenderer.endColor = endColor;

        lightningRenderer.enabled = true;
        lightningHideTime = Time.unscaledTime + Mathf.Max(0.01f, lightningFlashDuration);
        ScheduleNextLightningFlash();
        PlayThunder();
    }

    void ScheduleNextLightningFlash()
    {
        float minInterval = Mathf.Max(0.05f, lightningMinInterval);
        float maxInterval = Mathf.Max(minInterval, lightningMaxInterval);
        nextLightningTime = Time.unscaledTime + UnityEngine.Random.Range(minInterval, maxInterval);
    }

    void PlayThunder()
    {
        if (thunderClips == null || thunderClips.Length == 0)
            return;

        int index = UnityEngine.Random.Range(0, thunderClips.Length);
        AudioClip clip = thunderClips[index];
        if (clip == null)
            return;

        if (thunderAudio != null)
        {
            thunderAudio.PlayOneShot(clip, thunderVolume);
            return;
        }

        Vector3 playPosition = transform.position;
        if (Camera.main != null)
            playPosition = Camera.main.transform.position;

        AudioSource.PlayClipAtPoint(clip, playPosition, thunderVolume);
    }

    static AudioClip[] CollectThunderClipsFromTenkoku(TenkokuLightningFX tenkokuLightning)
    {
        if (tenkokuLightning == null)
            return Array.Empty<AudioClip>();

        List<AudioClip> clips = new List<AudioClip>(16);
        AppendThunderClips(clips, tenkokuLightning.audioThunderDist);
        AppendThunderClips(clips, tenkokuLightning.audioThunderMed);
        AppendThunderClips(clips, tenkokuLightning.audioThunderNear);
        return clips.ToArray();
    }

    static void AppendThunderClips(List<AudioClip> destination, AudioClip[] source)
    {
        if (destination == null || source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            AudioClip clip = source[i];
            if (clip != null)
                destination.Add(clip);
        }
    }
}
