using System;
using TMPro;
using UnityEngine;

public class WeatherExperienceController : MonoBehaviour
{
    enum WeatherVisualState
    {
        Unknown,
        Clear,
        Cloudy,
        Rain,
        Snow,
        Storm
    }

    [Header("Core References")]
    [SerializeField] private SimulatedTimeController simulatedTime;
    [SerializeField] private OpenMeteoForecastService forecastService;

    [Header("World Space UI")]
    [SerializeField] private TMP_Text dateLabel;
    [SerializeField] private TMP_Text relativeDayLabel;
    [SerializeField] private TMP_Text hourLabel;
    [SerializeField] private TMP_Text weatherLabel;
    [SerializeField] private TMP_Text temperatureLabel;
    [SerializeField] private TMP_Text statusLabel;

    [Header("Weather Effects")]
    [SerializeField] private GameObject clearWeatherRoot;
    [SerializeField] private GameObject cloudyWeatherRoot;
    [SerializeField] private GameObject rainWeatherRoot;
    [SerializeField] private GameObject snowWeatherRoot;
    [SerializeField] private GameObject stormWeatherRoot;

    [Header("Lighting")]
    [SerializeField] private Light weatherLight;
    [SerializeField] private Color dayLightColor = new Color(1f, 0.95f, 0.85f);
    [SerializeField] private Color nightLightColor = new Color(0.25f, 0.35f, 0.6f);
    [SerializeField] private Color overcastTint = new Color(0.75f, 0.82f, 0.9f);
    [SerializeField] private float dayLightIntensity = 1.2f;
    [SerializeField] private float nightLightIntensity = 0.25f;

    [Header("Ambient Audio")]
    [SerializeField] private AudioSource clearAudio;
    [SerializeField] private AudioSource cloudyAudio;
    [SerializeField] private AudioSource rainAudio;
    [SerializeField] private AudioSource snowAudio;
    [SerializeField] private AudioSource stormAudio;
    [SerializeField] private float ambienceFadeSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float activeAmbienceVolume = 0.85f;

    [Header("Optional Controllers")]
    [SerializeField] private StormLightningController stormLightningController;

    DateTime latestSimulatedDateTime;
    bool hasSimulatedTime;
    WeatherVisualState activeState = WeatherVisualState.Unknown;

    float clearTargetVolume;
    float cloudyTargetVolume;
    float rainTargetVolume;
    float snowTargetVolume;
    float stormTargetVolume;

    void OnEnable()
    {
        ConfigureStormLightningController();

        if (simulatedTime != null)
            simulatedTime.SimulatedDateTimeChanged += HandleSimulatedTimeChanged;

        if (forecastService != null)
        {
            forecastService.onForecastUpdated.AddListener(RefreshFromCurrentTime);
            forecastService.onRequestFailed.AddListener(HandleRequestFailed);
            forecastService.onStatusChanged.AddListener(SetStatus);
        }

        if (forecastService != null)
            forecastService.RefreshForecast();

        if (simulatedTime != null)
            HandleSimulatedTimeChanged(simulatedTime.SimulatedDateTime);
    }

    void OnDisable()
    {
        if (simulatedTime != null)
            simulatedTime.SimulatedDateTimeChanged -= HandleSimulatedTimeChanged;

        if (forecastService != null)
        {
            forecastService.onForecastUpdated.RemoveListener(RefreshFromCurrentTime);
            forecastService.onRequestFailed.RemoveListener(HandleRequestFailed);
            forecastService.onStatusChanged.RemoveListener(SetStatus);
        }
    }

    void Update()
    {
        FadeAmbience(clearAudio, clearTargetVolume);
        FadeAmbience(cloudyAudio, cloudyTargetVolume);
        FadeAmbience(rainAudio, rainTargetVolume);
        FadeAmbience(snowAudio, snowTargetVolume);
        FadeAmbience(stormAudio, stormTargetVolume);
    }

    public void NextDay()
    {
        if (simulatedTime != null)
            simulatedTime.NextDay();
    }

    public void PreviousDay()
    {
        if (simulatedTime != null)
            simulatedTime.PreviousDay();
    }

    public void ResetToToday()
    {
        if (simulatedTime != null)
            simulatedTime.ResetToToday();
    }

    public void ForceRefreshForecast()
    {
        if (forecastService != null)
            forecastService.RefreshForecast();
    }

    void ConfigureStormLightningController()
    {
        if (stormLightningController == null)
            stormLightningController = GetComponent<StormLightningController>();

        if (stormLightningController == null)
            stormLightningController = gameObject.AddComponent<StormLightningController>();

        stormLightningController.SetStormWeatherRoot(stormWeatherRoot);
    }

    void HandleSimulatedTimeChanged(DateTime dateTime)
    {
        latestSimulatedDateTime = dateTime;
        hasSimulatedTime = true;

        if (dateLabel != null)
            dateLabel.text = dateTime.ToString("dd/MM/yyyy");

        if (relativeDayLabel != null && simulatedTime != null)
            relativeDayLabel.text = simulatedTime.GetRelativeDayLabel();

        if (hourLabel != null)
            hourLabel.text = dateTime.ToString("HH:mm");

        RefreshFromCurrentTime();
    }

    void RefreshFromCurrentTime()
    {
        if (!hasSimulatedTime || forecastService == null)
            return;

        OpenMeteoForecastService.HourlyWeatherSample sample;
        bool hasSample = forecastService.TryGetHourlyWeather(latestSimulatedDateTime, out sample);

        if (!hasSample)
        {
            SetStatus("Pas de donnees meteo pour cette heure");
            ApplyWeatherState(WeatherVisualState.Unknown);
            UpdateWeatherLabels(WeatherVisualState.Unknown, float.NaN);
            UpdateWeatherLight(WeatherVisualState.Unknown, latestSimulatedDateTime);
            return;
        }

        WeatherVisualState state = MapWeatherCode(sample.weatherCode);

        ApplyWeatherState(state);
        UpdateWeatherLabels(state, sample.temperatureC);
        UpdateWeatherLight(state, latestSimulatedDateTime);
        SetStatus("Forecast active");
    }

    void HandleRequestFailed(string error)
    {
        SetStatus(error);
    }

    void SetStatus(string status)
    {
        if (statusLabel != null)
            statusLabel.text = status;
    }

    void UpdateWeatherLabels(WeatherVisualState state, float temperatureC)
    {
        if (weatherLabel != null)
            weatherLabel.text = WeatherLabel(state);

        if (temperatureLabel != null)
            temperatureLabel.text = float.IsNaN(temperatureC) ? "--" : temperatureC.ToString("0.0") + " C";
    }

    void ApplyWeatherState(WeatherVisualState state)
    {
        if (activeState == state)
            return;

        activeState = state;

        SetGameObjectActive(clearWeatherRoot, state == WeatherVisualState.Clear);
        SetGameObjectActive(cloudyWeatherRoot, state == WeatherVisualState.Cloudy);
        SetGameObjectActive(rainWeatherRoot, state == WeatherVisualState.Rain);
        SetGameObjectActive(snowWeatherRoot, state == WeatherVisualState.Snow);
        SetGameObjectActive(stormWeatherRoot, state == WeatherVisualState.Storm);

        clearTargetVolume = state == WeatherVisualState.Clear ? activeAmbienceVolume : 0f;
        cloudyTargetVolume = state == WeatherVisualState.Cloudy ? activeAmbienceVolume : 0f;
        rainTargetVolume = state == WeatherVisualState.Rain ? activeAmbienceVolume : 0f;
        snowTargetVolume = state == WeatherVisualState.Snow ? activeAmbienceVolume : 0f;
        stormTargetVolume = state == WeatherVisualState.Storm ? activeAmbienceVolume : 0f;
    }

    void UpdateWeatherLight(WeatherVisualState state, DateTime dateTime)
    {
        if (weatherLight == null)
            return;

        float hour = dateTime.Hour + (dateTime.Minute / 60f);
        float daylight = Mathf.Clamp01(Mathf.Sin(((hour - 6f) / 12f) * Mathf.PI));

        float weatherIntensityMultiplier = 1f;
        if (state == WeatherVisualState.Cloudy)
            weatherIntensityMultiplier = 0.82f;
        else if (state == WeatherVisualState.Rain)
            weatherIntensityMultiplier = 0.7f;
        else if (state == WeatherVisualState.Snow)
            weatherIntensityMultiplier = 0.76f;
        else if (state == WeatherVisualState.Storm)
            weatherIntensityMultiplier = 0.58f;
        else if (state == WeatherVisualState.Unknown)
            weatherIntensityMultiplier = 0.64f;

        weatherLight.intensity = Mathf.Lerp(nightLightIntensity, dayLightIntensity, daylight) * weatherIntensityMultiplier;

        Color baseColor = Color.Lerp(nightLightColor, dayLightColor, daylight);
        float overcastMix = state == WeatherVisualState.Clear ? 0f : 0.4f;
        weatherLight.color = Color.Lerp(baseColor, overcastTint, overcastMix);
    }

    void FadeAmbience(AudioSource source, float targetVolume)
    {
        if (source == null)
            return;

        if (targetVolume > 0f && !source.isPlaying)
            source.Play();

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, ambienceFadeSpeed * Time.deltaTime);

        if (source.volume <= 0.001f && targetVolume <= 0f && source.isPlaying)
            source.Stop();
    }

    static void SetGameObjectActive(GameObject root, bool value)
    {
        if (root != null && root.activeSelf != value)
            root.SetActive(value);
    }

    static string WeatherLabel(WeatherVisualState state)
    {
        if (state == WeatherVisualState.Clear)
            return "Ensoleille";
        if (state == WeatherVisualState.Cloudy)
            return "Nuageux";
        if (state == WeatherVisualState.Rain)
            return "Pluie";
        if (state == WeatherVisualState.Snow)
            return "Neige";
        if (state == WeatherVisualState.Storm)
            return "Orage";
        if (state == WeatherVisualState.Unknown)
            return "Donnees indisponibles";

        return "Inconnu";
    }

    static WeatherVisualState MapWeatherCode(int code)
    {
        if (code == 0)
            return WeatherVisualState.Clear;

        if ((code >= 1 && code <= 3) || code == 45 || code == 48)
            return WeatherVisualState.Cloudy;

        if ((code >= 51 && code <= 67) || (code >= 80 && code <= 82))
            return WeatherVisualState.Rain;

        if ((code >= 71 && code <= 77) || (code >= 85 && code <= 86))
            return WeatherVisualState.Snow;

        if (code >= 95)
            return WeatherVisualState.Storm;

        return WeatherVisualState.Unknown;
    }
}
