using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class OpenMeteoForecastService : MonoBehaviour
{
    [Serializable]
    public struct HourlyWeatherSample
    {
        public string timeKey;
        public float temperatureC;
        public int weatherCode;
    }

    [Header("Location")]
    [SerializeField] private float latitude = 48.428f;
    [SerializeField] private float longitude = -71.067f;
    [SerializeField] private string timezone = "auto";

    [Header("Request")]
    [SerializeField, Range(1, 7)] private int forecastDays = 7;
    [SerializeField] private bool autoFetchOnEnable = true;
    [SerializeField, Min(1f)] private float requestTimeoutSeconds = 5f;

    [Header("Retry")]
    [SerializeField, Range(0, 3)] private int maxRetryCount = 2;
    [SerializeField, Min(0.25f)] private float retryDelaySeconds = 1f;

    [Header("Events")]
    public UnityEvent onForecastUpdated = new UnityEvent();
    public UnityEvent<string> onRequestFailed = new UnityEvent<string>();
    public UnityEvent<string> onStatusChanged = new UnityEvent<string>();

    readonly Dictionary<string, HourlyWeatherSample> hourlyByKey = new Dictionary<string, HourlyWeatherSample>();

    Coroutine requestRoutine;

    void OnEnable()
    {
        if (autoFetchOnEnable)
            RefreshForecast();
    }

    public void SetCoordinates(float newLatitude, float newLongitude)
    {
        latitude = newLatitude;
        longitude = newLongitude;
    }

    public void RefreshForecast()
    {
        if (requestRoutine != null)
            return;

        requestRoutine = StartCoroutine(FetchForecastCoroutine());
    }

    public bool TryGetHourlyWeather(DateTime localDateTime, out HourlyWeatherSample sample)
    {
        string key = FormatHourKey(localDateTime);
        return hourlyByKey.TryGetValue(key, out sample);
    }

    public bool TryGetHourlyWeather(string hourKey, out HourlyWeatherSample sample)
    {
        return hourlyByKey.TryGetValue(hourKey, out sample);
    }

    public void ClearCache()
    {
        hourlyByKey.Clear();
    }

    IEnumerator FetchForecastCoroutine()
    {
        string url = BuildForecastUrl();
        int totalAttempts = maxRetryCount + 1;
        string lastError = "Open-Meteo request failed";

        for (int attempt = 0; attempt < totalAttempts; attempt++)
        {
            onStatusChanged.Invoke(string.Format(CultureInfo.InvariantCulture, "Fetching weather from Open-Meteo ({0}/{1})", attempt + 1, totalAttempts));

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = Mathf.RoundToInt(requestTimeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    OpenMeteoForecastResponse response = JsonUtility.FromJson<OpenMeteoForecastResponse>(request.downloadHandler.text);
                    string parseError;
                    if (TryBuildHourlyIndex(response, out parseError))
                    {
                        requestRoutine = null;
                        onStatusChanged.Invoke("Forecast updated");
                        onForecastUpdated.Invoke();
                        yield break;
                    }

                    lastError = "Open-Meteo parse failed: " + parseError;
                }
                else
                {
                    lastError = "Open-Meteo request failed: " + request.error;
                }
            }

            bool canRetry = attempt + 1 < totalAttempts;
            if (!canRetry)
                break;

            onStatusChanged.Invoke(lastError + " - retrying...");
            yield return new WaitForSeconds(retryDelaySeconds);
        }

        requestRoutine = null;

        onStatusChanged.Invoke(lastError);
        onRequestFailed.Invoke(lastError);
    }

    string BuildForecastUrl()
    {
        string escapedTimezone = UnityWebRequest.EscapeURL(timezone);

        return string.Format(
            CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&hourly=temperature_2m,weather_code&timezone={2}&forecast_days={3}",
            latitude,
            longitude,
            escapedTimezone,
            forecastDays);
    }

    bool TryBuildHourlyIndex(OpenMeteoForecastResponse response, out string error)
    {
        error = string.Empty;

        if (response == null || response.hourly == null)
        {
            error = "Missing hourly payload";
            return false;
        }

        if (response.hourly.time == null || response.hourly.temperature_2m == null || response.hourly.weather_code == null)
        {
            error = "Hourly arrays are incomplete";
            return false;
        }

        int sampleCount = Mathf.Min(
            response.hourly.time.Length,
            Mathf.Min(response.hourly.temperature_2m.Length, response.hourly.weather_code.Length));

        if (sampleCount == 0)
        {
            error = "No hourly samples available";
            return false;
        }

        hourlyByKey.Clear();

        for (int i = 0; i < sampleCount; i++)
        {
            string key = NormalizeApiHourKey(response.hourly.time[i]);
            if (string.IsNullOrEmpty(key))
                continue;

            HourlyWeatherSample sample = new HourlyWeatherSample
            {
                timeKey = key,
                temperatureC = response.hourly.temperature_2m[i],
                weatherCode = response.hourly.weather_code[i]
            };

            hourlyByKey[key] = sample;
        }

        return hourlyByKey.Count > 0;
    }

    static string NormalizeApiHourKey(string rawTime)
    {
        if (string.IsNullOrEmpty(rawTime))
            return string.Empty;

        int tIndex = rawTime.IndexOf('T');
        if (tIndex < 0 || rawTime.Length < tIndex + 3)
            return rawTime;

        return rawTime.Substring(0, tIndex + 3) + ":00";
    }

    public static string FormatHourKey(DateTime localDateTime)
    {
        return localDateTime.ToString("yyyy-MM-dd'T'HH':00'");
    }

    [Serializable]
    class OpenMeteoForecastResponse
    {
        public OpenMeteoHourlyPayload hourly;
    }

    [Serializable]
    class OpenMeteoHourlyPayload
    {
        public string[] time;
        public float[] temperature_2m;
        public int[] weather_code;
    }
}
