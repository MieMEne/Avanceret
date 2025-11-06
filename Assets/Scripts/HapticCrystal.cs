using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class HapticCrystal : MonoBehaviour
{
    [Header("Haptic Settings")]
    [Range(0f, 1f)]
    public float hapticAmplitude = 0.5f; // Strength of vibration
    public float hapticDuration = 0.2f;  // Duration in seconds

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Try to get XRController from interactor
        XRController controller = args.interactorObject.transform.GetComponent<XRController>();

        if (controller != null)
        {
            TryLowLevelHaptic(controller.controllerNode, hapticAmplitude, hapticDuration);
        }

        // Optional: destroy the crystal after pickup
        Destroy(gameObject);
    }

    private void TryLowLevelHaptic(XRNode node, float amplitude, float duration)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return;

        if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
        }
    }
}
