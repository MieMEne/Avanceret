using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(XRGrabInteractable))]
public class PickupSound : MonoBehaviour
{
    public AudioClip pickupSound;
    public float hapticAmplitude = 0.5f; // strength of vibration
    public float hapticDuration = 0.2f;  // duration of vibration in seconds

    private AudioSource audioSource;
    private XRGrabInteractable grabInteractable;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Play audio
        audioSource.PlayOneShot(pickupSound);

        // Send haptic feedback
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
        {
            XRBaseController controller = controllerInteractor.xrController;
            if (controller != null)
            {
                controller.SendHapticImpulse(hapticAmplitude, hapticDuration);
            }
        }
    }
}
