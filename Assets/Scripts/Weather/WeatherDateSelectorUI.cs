using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeatherDateSelectorUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private SimulatedTimeController simulatedTime;

    [Header("UI References")]
    [SerializeField] private TMP_Text relativeDayLabel;
    [SerializeField] private TMP_Text absoluteDateLabel;
    [SerializeField] private Button previousDayButton;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button todayButton;
    [SerializeField] private Slider daySlider;

    bool isBindingSlider;

    void OnEnable()
    {
        if (simulatedTime != null)
        {
            simulatedTime.SimulatedDateTimeChanged += HandleDateTimeChanged;
            simulatedTime.onCanGoPreviousDayChanged.AddListener(SetPreviousEnabled);
            simulatedTime.onCanGoNextDayChanged.AddListener(SetNextEnabled);
            simulatedTime.onRelativeDayLabelChanged.AddListener(SetRelativeDayLabel);
        }

        if (previousDayButton != null)
            previousDayButton.onClick.AddListener(GoPrevious);

        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(GoNext);

        if (todayButton != null)
            todayButton.onClick.AddListener(GoToday);

        if (daySlider != null)
        {
            daySlider.wholeNumbers = true;
            daySlider.onValueChanged.AddListener(OnSliderChanged);
        }

        RefreshUI();
    }

    void OnDisable()
    {
        if (simulatedTime != null)
        {
            simulatedTime.SimulatedDateTimeChanged -= HandleDateTimeChanged;
            simulatedTime.onCanGoPreviousDayChanged.RemoveListener(SetPreviousEnabled);
            simulatedTime.onCanGoNextDayChanged.RemoveListener(SetNextEnabled);
            simulatedTime.onRelativeDayLabelChanged.RemoveListener(SetRelativeDayLabel);
        }

        if (previousDayButton != null)
            previousDayButton.onClick.RemoveListener(GoPrevious);

        if (nextDayButton != null)
            nextDayButton.onClick.RemoveListener(GoNext);

        if (todayButton != null)
            todayButton.onClick.RemoveListener(GoToday);

        if (daySlider != null)
            daySlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    public void SetSimulatedTimeController(SimulatedTimeController value)
    {
        simulatedTime = value;
        RefreshUI();
    }

    public void GoPrevious()
    {
        if (simulatedTime != null)
            simulatedTime.PreviousDay();
    }

    public void GoNext()
    {
        if (simulatedTime != null)
            simulatedTime.NextDay();
    }

    public void GoToday()
    {
        if (simulatedTime != null)
            simulatedTime.ResetToToday();
    }

    void OnSliderChanged(float value)
    {
        if (isBindingSlider || simulatedTime == null)
            return;

        simulatedTime.SetDayOffsetFromSlider(value);
    }

    void HandleDateTimeChanged(System.DateTime dateTime)
    {
        if (absoluteDateLabel != null)
            absoluteDateLabel.text = dateTime.ToString("dd/MM/yyyy");

        RefreshButtonState();
        RefreshSlider();
    }

    void RefreshUI()
    {
        if (simulatedTime == null)
            return;

        HandleDateTimeChanged(simulatedTime.SimulatedDateTime);
        SetRelativeDayLabel(simulatedTime.GetRelativeDayLabel());
    }

    void RefreshButtonState()
    {
        if (simulatedTime == null)
            return;

        SetPreviousEnabled(simulatedTime.CanGoPreviousDay());
        SetNextEnabled(simulatedTime.CanGoNextDay());

        if (todayButton != null)
            todayButton.interactable = simulatedTime.DayOffset != 0;
    }

    void RefreshSlider()
    {
        if (simulatedTime == null || daySlider == null)
            return;

        isBindingSlider = true;
        daySlider.minValue = simulatedTime.MinDayOffset;
        daySlider.maxValue = simulatedTime.MaxDayOffset;
        daySlider.SetValueWithoutNotify(simulatedTime.DayOffset);
        isBindingSlider = false;
    }

    void SetRelativeDayLabel(string value)
    {
        if (relativeDayLabel != null)
            relativeDayLabel.text = value;
    }

    void SetPreviousEnabled(bool enabled)
    {
        if (previousDayButton != null)
            previousDayButton.interactable = enabled;
    }

    void SetNextEnabled(bool enabled)
    {
        if (nextDayButton != null)
            nextDayButton.interactable = enabled;
    }
}
