using System;
using UnityEngine;
using UnityEngine.Events;

public class SimulatedTimeController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private SwipeRotateSphere sphereInput;
    [SerializeField] private bool syncWithSphereOnEnable = true;

    [Header("Date Range")]
    [SerializeField, Range(0, 6)] private int dayOffset;
    [SerializeField, Range(0, 6)] private int maxDayOffset = 6;

    [Header("Fallback")]
    [SerializeField, Range(0f, 24f)] private float fallbackHour = 12f;

    [Header("Events")]
    public UnityEvent<int> onDayOffsetChanged = new UnityEvent<int>();
    public UnityEvent<float> onHourChanged = new UnityEvent<float>();
    public UnityEvent<string> onDateLabelChanged = new UnityEvent<string>();
    public UnityEvent<string> onTimeLabelChanged = new UnityEvent<string>();
    public UnityEvent<string> onRelativeDayLabelChanged = new UnityEvent<string>();
    public UnityEvent<bool> onCanGoPreviousDayChanged = new UnityEvent<bool>();
    public UnityEvent<bool> onCanGoNextDayChanged = new UnityEvent<bool>();
    public UnityEvent<string> onIsoHourKeyChanged = new UnityEvent<string>();

    public event Action<DateTime> SimulatedDateTimeChanged;

    public int MinDayOffset => 0;
    public int MaxDayOffset => maxDayOffset;
    public int DayOffset => dayOffset;
    public float HourContinuous { get; private set; }
    public DateTime SimulatedDateTime { get; private set; }

    void OnEnable()
    {
        if (sphereInput != null)
            sphereInput.onHourContinuousChanged.AddListener(SetHourContinuous);

        HourContinuous = Mathf.Repeat(fallbackHour, 24f);
        if (syncWithSphereOnEnable && sphereInput != null)
            HourContinuous = sphereInput.CurrentHourContinuous;

        PublishState();
    }

    void OnDisable()
    {
        if (sphereInput != null)
            sphereInput.onHourContinuousChanged.RemoveListener(SetHourContinuous);
    }

    public void SetSphereInput(SwipeRotateSphere input)
    {
        if (sphereInput != null)
            sphereInput.onHourContinuousChanged.RemoveListener(SetHourContinuous);

        sphereInput = input;

        if (isActiveAndEnabled && sphereInput != null)
            sphereInput.onHourContinuousChanged.AddListener(SetHourContinuous);
    }

    public void NextDay()
    {
        SetDayOffset(dayOffset + 1);
    }

    public void PreviousDay()
    {
        SetDayOffset(dayOffset - 1);
    }

    public void ResetToToday()
    {
        SetDayOffset(0);
    }

    public void SetDayOffset(int value)
    {
        int clamped = Mathf.Clamp(value, MinDayOffset, maxDayOffset);
        if (clamped == dayOffset)
            return;

        dayOffset = clamped;
        PublishState();
    }

    public void SetDayOffsetFromSlider(float value)
    {
        SetDayOffset(Mathf.RoundToInt(value));
    }

    public bool CanGoPreviousDay()
    {
        return dayOffset > MinDayOffset;
    }

    public bool CanGoNextDay()
    {
        return dayOffset < maxDayOffset;
    }

    public string GetRelativeDayLabel()
    {
        if (dayOffset == 0)
            return "Aujourd'hui";
        if (dayOffset == 1)
            return "Demain";

        return "J+" + dayOffset;
    }

    public void SetHourContinuous(float hour)
    {
        float wrappedHour = Mathf.Repeat(hour, 24f);
        if (Mathf.Approximately(wrappedHour, HourContinuous))
            return;

        HourContinuous = wrappedHour;
        PublishState();
    }

    void PublishState()
    {
        DateTime baseDate = DateTime.Today.AddDays(dayOffset);

        int hour = Mathf.FloorToInt(HourContinuous) % 24;
        float hourFraction = HourContinuous - hour;
        int minute = Mathf.Clamp(Mathf.FloorToInt(hourFraction * 60f), 0, 59);

        SimulatedDateTime = new DateTime(
            baseDate.Year,
            baseDate.Month,
            baseDate.Day,
            hour,
            minute,
            0,
            DateTimeKind.Local);

        onDayOffsetChanged.Invoke(dayOffset);
        onHourChanged.Invoke(HourContinuous);
        onDateLabelChanged.Invoke(SimulatedDateTime.ToString("dd/MM/yyyy"));
        onTimeLabelChanged.Invoke(SimulatedDateTime.ToString("HH:mm"));
        onRelativeDayLabelChanged.Invoke(GetRelativeDayLabel());
        onCanGoPreviousDayChanged.Invoke(CanGoPreviousDay());
        onCanGoNextDayChanged.Invoke(CanGoNextDay());
        onIsoHourKeyChanged.Invoke(OpenMeteoForecastService.FormatHourKey(SimulatedDateTime));

        SimulatedDateTimeChanged?.Invoke(SimulatedDateTime);
    }
}
