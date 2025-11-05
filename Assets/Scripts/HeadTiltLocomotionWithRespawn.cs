using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit; // for XRBaseController haptics

[RequireComponent(typeof(CharacterController))]
public class HeadTiltLocomotionWithRespawn : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your HMD camera (Main Camera under XR Origin).")]
    public Transform head;

    [Header("Speeds")]
    public float forwardSpeed = 5f;
    public float maxStrafeSpeed = 2f;

    [Header("Tilt Settings")]
    public float deadZoneDegrees = 3f;
    public float maxTiltDegrees = 20f;
    public float inputSmoothing = 8f;

    [Header("Direction")]
    public bool forwardFollowsHead = true;

    [Header("Respawn")]
    [Tooltip("Optional: set a custom spawn transform. If empty, we use the scene start pose.")]
    public Transform customSpawnPoint;

    [Header("Haptics On Hit")]
    public bool hapticsOnHit = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.6f;
    public float hapticDuration = 0.12f;
    public float hapticRetriggerCooldown = 0.2f;

    [Tooltip("Preferred: your Action-based Left/Right controllers (XRBaseController).")]
    public XRBaseController leftController;
    public XRBaseController rightController;

    [Header("Debug")]
    public bool printRollInConsole = false;

    CharacterController _cc;
    float _neutralRollDeg = 0f;
    float _strafeInputSmoothed = 0f;
    Vector3 _startPos;
    Quaternion _startRot;
    float _nextHapticTime = 0f;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (!head)
        {
            var cam = Camera.main;
            if (cam) head = cam.transform;
        }
    }

    void Start()
    {
        // Ensure the capsule isn't half in the ground
        if (_cc.center.y < 0.001f)
            _cc.center = new Vector3(_cc.center.x, _cc.height * 0.5f, _cc.center.z);

        // Record spawn pose
        _startPos = customSpawnPoint ? customSpawnPoint.position : transform.position;
        _startRot = customSpawnPoint ? customSpawnPoint.rotation : transform.rotation;

        CalibrateNeutralTilt();
    }

    void Update()
    {
        if (!head) return;

        // 1) Read tilt (roll) robustly
        float rawRoll = GetGravityRelativeRollDegrees(head);        // [-180..180], right-ear-up positive
        float rollFromNeutral = rawRoll - _neutralRollDeg;

        if (printRollInConsole)
            Debug.Log($"[Tilt] raw:{rawRoll:F1}°, neutral:{_neutralRollDeg:F1}°, fromNeutral:{rollFromNeutral:F1}°");

        // 2) Map to strafe input
        float strafeInput = RollToInput(rollFromNeutral, deadZoneDegrees, maxTiltDegrees);
        _strafeInputSmoothed = Mathf.Lerp(
            _strafeInputSmoothed,
            strafeInput,
            1f - Mathf.Exp(-inputSmoothing * Time.deltaTime)
        );

        // 3) Compute movement (XZ only)
        Vector3 forwardDir = forwardFollowsHead ? Flatten(head.forward).normalized : Vector3.forward;
        Vector3 rightDir = Vector3.Cross(Vector3.up, Vector3.Cross(forwardDir, Vector3.up)).normalized;

        Vector3 velocity = forwardDir * forwardSpeed + rightDir * (_strafeInputSmoothed * maxStrafeSpeed);

        // 4) Apply with CharacterController
        _cc.Move(velocity * Time.deltaTime);

#if UNITY_EDITOR
        // Handy editor key: press R to recalibrate neutral
        if (Input.GetKeyDown(KeyCode.R)) CalibrateNeutralTilt();
#endif
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider != null && hit.collider.CompareTag("Obstacle"))
        {
            TriggerHaptics();
            Respawn();
        }
    }

    public void Respawn()
    {
        _cc.enabled = false;
        transform.SetPositionAndRotation(_startPos, _startRot);
        _cc.enabled = true;

        _strafeInputSmoothed = 0f;
        CalibrateNeutralTilt();
    }

    public void CalibrateNeutralTilt()
    {
        if (!head) return;
        _neutralRollDeg = GetGravityRelativeRollDegrees(head);
        if (printRollInConsole)
            Debug.Log($"[Tilt] Calibrated neutral = {_neutralRollDeg:F1}°");
    }

    // ---------------- HAPTICS ----------------

    void TriggerHaptics()
    {
        if (!hapticsOnHit) return;
        if (Time.time < _nextHapticTime) return;
        _nextHapticTime = Time.time + hapticRetriggerCooldown;

        bool usedXRI = false;

        // Preferred: XR Interaction Toolkit controllers
        if (leftController)
        {
            leftController.SendHapticImpulse(Mathf.Clamp01(hapticAmplitude), Mathf.Max(0f, hapticDuration));
            usedXRI = true;
        }
        if (rightController)
        {
            rightController.SendHapticImpulse(Mathf.Clamp01(hapticAmplitude), Mathf.Max(0f, hapticDuration));
            usedXRI = true;
        }

        // Fallback to low-level XR API if XRI controllers not assigned
        if (!usedXRI)
        {
            TryLowLevelHaptic(XRNode.LeftHand, hapticAmplitude, hapticDuration);
            TryLowLevelHaptic(XRNode.RightHand, hapticAmplitude, hapticDuration);
        }
    }

    static void TryLowLevelHaptic(XRNode node, float amplitude, float duration)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return;

        if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
        }
    }

    // ---------------- HELPERS ----------------

    // Robust roll: signed angle between world up and head up around the head's forward axis.
    // Right ear up -> positive; left ear up -> negative.
    static float GetGravityRelativeRollDegrees(Transform h)
    {
        float roll = Vector3.SignedAngle(Vector3.up, h.up, h.forward);
        if (roll > 180f) roll -= 360f;
        if (roll < -180f) roll += 360f;
        return roll;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static float RollToInput(float rollDeg, float deadZoneDeg, float maxDeg)
    {
        if (Mathf.Abs(rollDeg) <= deadZoneDeg) return 0f;
        float sign = Mathf.Sign(rollDeg);
        float mag = Mathf.Clamp01((Mathf.Abs(rollDeg) - deadZoneDeg) / Mathf.Max(0.0001f, (maxDeg - deadZoneDeg)));
        return sign * mag; // -1..+1
    }
}
