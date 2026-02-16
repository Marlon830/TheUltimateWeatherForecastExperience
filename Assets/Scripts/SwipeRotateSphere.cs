using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SwipeRotateSphere : MonoBehaviour
{
    public float rotationSpeed = 200f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private Transform interactorTransform;
    private Vector3 lastInteractorPos;
    private bool isInteracting = false;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        interactorTransform = args.interactorObject.transform;
        lastInteractorPos = interactorTransform.position;
        isInteracting = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isInteracting = false;
    }

    void Update()
    {
        if (!isInteracting || interactorTransform == null)
            return;

        Vector3 currentPos = interactorTransform.position;
        Vector3 delta = currentPos - lastInteractorPos;

        float horizontal = delta.x;

        transform.Rotate(Vector3.up, -horizontal * rotationSpeed, Space.World);

        lastInteractorPos = currentPos;
    }
}
