using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SwipeRotateSphere : MonoBehaviour
{
    public float rotationSpeed = 200f;
    [Range(0f, 24f)] public float initialHour = 12f;

    [Header("Feedback")]
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioClip grabAudioClip;
    [SerializeField] private AudioClip releaseAudioClip;
    [SerializeField, Range(0f, 1f)] private float grabHapticIntensity = 0.45f;
    [SerializeField, Min(0f)] private float grabHapticDuration = 0.06f;
    [SerializeField, Range(0f, 1f)] private float releaseHapticIntensity = 0.22f;
    [SerializeField, Min(0f)] private float releaseHapticDuration = 0.04f;
    [SerializeField] private bool sendHourTickHaptics = true;
    [SerializeField, Range(0f, 1f)] private float hourTickHapticIntensity = 0.08f;
    [SerializeField, Min(0f)] private float hourTickHapticDuration = 0.02f;

    public UnityEvent onInteractionStarted = new UnityEvent();
    public UnityEvent onInteractionEnded = new UnityEvent();
    public UnityEvent<float> onHourContinuousChanged = new UnityEvent<float>();
    public UnityEvent<int> onHourChanged = new UnityEvent<int>();

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private Transform interactorTransform;
    private Vector3 lastInteractorPos;
    private bool isInteracting = false;
    private float accumulatedRotationDegrees;
    private float currentHourContinuous;
    private int currentHour;
    private object activeInteractorObject;

    public float CurrentHourContinuous => currentHourContinuous;
    public int CurrentHour => currentHour;

    const float k_DegreesPerDay = 360f;
    const float k_HoursPerDay = 24f;
    const float k_DegreesPerHour = k_DegreesPerDay / k_HoursPerDay;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        currentHourContinuous = Mathf.Repeat(initialHour, k_HoursPerDay);
        currentHour = Mathf.FloorToInt(currentHourContinuous) % 24;
        accumulatedRotationDegrees = currentHourContinuous * k_DegreesPerHour;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnEnable()
    {
        NotifyTimeChanged();
    }

    void OnDestroy()
    {
        if (grab == null)
            return;

        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        interactorTransform = args.interactorObject.transform;
        lastInteractorPos = interactorTransform.position;
        isInteracting = true;
        activeInteractorObject = args.interactorObject;

        PlayFeedbackClip(grabAudioClip);
        TrySendHaptics(args.interactorObject, grabHapticIntensity, grabHapticDuration);
        onInteractionStarted.Invoke();
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isInteracting = false;

        PlayFeedbackClip(releaseAudioClip);
        TrySendHaptics(args.interactorObject, releaseHapticIntensity, releaseHapticDuration);

        activeInteractorObject = null;
        onInteractionEnded.Invoke();
    }

    void Update()
    {
        if (!isInteracting || interactorTransform == null)
            return;

        Vector3 currentPos = interactorTransform.position;
        Vector3 delta = currentPos - lastInteractorPos;

        float horizontal = delta.x;
        float appliedRotation = -horizontal * rotationSpeed;

        transform.Rotate(Vector3.up, appliedRotation, Space.World);
        accumulatedRotationDegrees = Mathf.Repeat(accumulatedRotationDegrees + appliedRotation, k_DegreesPerDay);
        UpdateHourFromRotation();

        lastInteractorPos = currentPos;
    }

    public void SetHourFromUI(float hour)
    {
        float wrappedHour = Mathf.Repeat(hour, k_HoursPerDay);
        float previousDegrees = accumulatedRotationDegrees;
        float newDegrees = wrappedHour * k_DegreesPerHour;
        float deltaDegrees = Mathf.DeltaAngle(previousDegrees, newDegrees);

        transform.Rotate(Vector3.up, deltaDegrees, Space.World);
        accumulatedRotationDegrees = Mathf.Repeat(newDegrees, k_DegreesPerDay);
        UpdateHourFromRotation();
    }

    void UpdateHourFromRotation()
    {
        float newContinuousHour = accumulatedRotationDegrees / k_DegreesPerHour;
        int newHour = Mathf.FloorToInt(newContinuousHour) % 24;

        bool continuousChanged = !Mathf.Approximately(newContinuousHour, currentHourContinuous);
        bool hourChanged = newHour != currentHour;

        currentHourContinuous = newContinuousHour;
        currentHour = newHour;

        if (hourChanged && sendHourTickHaptics && isInteracting)
            TrySendHaptics(activeInteractorObject, hourTickHapticIntensity, hourTickHapticDuration);

        if (continuousChanged || hourChanged)
            NotifyTimeChanged();
    }

    void NotifyTimeChanged()
    {
        onHourContinuousChanged.Invoke(currentHourContinuous);
        onHourChanged.Invoke(currentHour);
    }

    void PlayFeedbackClip(AudioClip clip)
    {
        if (feedbackAudioSource == null || clip == null)
            return;

        feedbackAudioSource.PlayOneShot(clip);
    }

    void TrySendHaptics(object interactorObject, float intensity, float duration)
    {
        if (interactorObject == null || intensity <= 0f || duration <= 0f)
            return;

        XRBaseInputInteractor inputInteractor = interactorObject as XRBaseInputInteractor;
        if (inputInteractor == null)
            return;

        inputInteractor.SendHapticImpulse(Mathf.Clamp01(intensity), duration);
    }
}
