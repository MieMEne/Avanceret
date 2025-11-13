using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Oculus.Haptics;

[RequireComponent(typeof(XRGrabInteractable))]
public class CrystalCollectible : MonoBehaviour
{
    [Header("Scoring")]
    [Tooltip("Base value. Positive for good, negative for bad (simplest way).")]
    public int value = 1;

    [Tooltip("If true, detect wrong crystal by Tag instead of using the 'value' sign.")]
    public bool detectWrongByTag = false;

    [Tooltip("Tag used to mark wrong crystals when detectWrongByTag is true.")]
    public string wrongTag = "WrongCrystal";

    [Tooltip("Optional: if not using tags, you can tick this on the wrong crystal instance.")]
    public bool isWrongCrystal = false;

    [Header("Feedback (optional)")]
    public AudioClip goodSound;
    public AudioClip badSound;
    public ParticleSystem goodVFX;
    public ParticleSystem badVFX;

    [Header("Haptics")]
    [Tooltip("If true and a Meta .haptic clip is assigned, uses Meta Haptics SDK.")]
    public bool useMetaHaptics = true;

    [Tooltip("Haptic pattern for good crystals (.haptic file from Meta Haptic Studio).")]
    public HapticClip goodHaptic;

    [Tooltip("Haptic pattern for bad crystals (.haptic file from Meta Haptic Studio).")]
    public HapticClip badHaptic;

    [Tooltip("Optional: Assign XR controller references (can still work without).")]
    public XRBaseController leftController;
    public XRBaseController rightController;

    [Range(0f, 10f)] public float hapticAmplitude = 0.7f;
    public float hapticDuration = 0.1f;

    private XRGrabInteractable grab;
    private bool collected = false;

    private HapticClipPlayer _hapticPlayer;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrabbed);

        if (_hapticPlayer != null)
        {
            _hapticPlayer.Dispose();
            _hapticPlayer = null;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (collected) return;
        collected = true;

        // Determine if crystal is wrong
        bool wrong;
        if (detectWrongByTag)
            wrong = CompareTag(wrongTag);
        else
            wrong = isWrongCrystal;

        // Calculate score delta
        int delta;
        if (detectWrongByTag || isWrongCrystal)
        {
            int mag = Mathf.Abs(value);
            delta = wrong ? -mag : mag;
        }
        else
        {
            delta = value;
        }

        // Add to collector
        if (CrystalCollector.Instance != null)
            CrystalCollector.Instance.Add(delta);
        else
            Debug.LogWarning("No CrystalCollector found in the scene!");

        // ✅ Play haptic feedback (based on correct/wrong)
        TriggerHaptics(args, wrong);

        // Sound + VFX feedback
        if (wrong)
        {
            if (badSound) AudioSource.PlayClipAtPoint(badSound, transform.position);
            if (badVFX) Instantiate(badVFX, transform.position, Quaternion.identity);
        }
        else
        {
            if (goodSound) AudioSource.PlayClipAtPoint(goodSound, transform.position);
            if (goodVFX) Instantiate(goodVFX, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, 0.1f); // small delay to let haptic play
    }

    // ---------------- HAPTICS ----------------
    private void TriggerHaptics(SelectEnterEventArgs args, bool wrong)
    {
        HapticClip clipToPlay = wrong ? badHaptic : goodHaptic;

        // 1️⃣ Try Meta Haptics first
        if (useMetaHaptics && clipToPlay != null)
        {
            if (_hapticPlayer != null)
            {
                _hapticPlayer.Dispose();
                _hapticPlayer = null;
            }

            _hapticPlayer = new HapticClipPlayer(clipToPlay);

            // Detect which hand grabbed it
            var interactor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor;
            if (interactor != null && interactor.xrController != null)
            {
                string name = interactor.xrController.name.ToLower();
                if (name.Contains("left"))
                    _hapticPlayer.Play(Controller.Left);
                else if (name.Contains("right"))
                    _hapticPlayer.Play(Controller.Right);
                else
                {
                    _hapticPlayer.Play(Controller.Left);
                    _hapticPlayer.Play(Controller.Right);
                }
            }
            else
            {
                // If no controller found, play on both
                _hapticPlayer.Play(Controller.Left);
                _hapticPlayer.Play(Controller.Right);
            }

            return;
        }

        // 2️⃣ Fallback: XR haptics
        if (leftController)
            leftController.SendHapticImpulse(hapticAmplitude, hapticDuration);
        if (rightController)
            rightController.SendHapticImpulse(hapticAmplitude, hapticDuration);

        // 3️⃣ Fallback low-level XR
        TryLowLevelHaptic(XRNode.LeftHand, hapticAmplitude, hapticDuration);
        TryLowLevelHaptic(XRNode.RightHand, hapticAmplitude, hapticDuration);
    }

    private static void TryLowLevelHaptic(XRNode node, float amplitude, float duration)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return;

        if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0u, amplitude, duration);
    }
}
