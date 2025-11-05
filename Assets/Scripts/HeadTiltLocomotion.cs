// HeadTiltLocomotion.cs
// Attach to your XR Origin (player root). Drag the HMD camera into "head".
using UnityEngine;

public class HeadTiltLocomotion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your HMD Camera (the Main Camera under XR Origin).")]
    public Transform head;

    [Header("Speeds")]
    [Tooltip("Constant forward speed in meters/second.")]
    public float forwardSpeed = 2.5f;
    [Tooltip("Maximum sideways speed in meters/second at max tilt.")]
    public float maxStrafeSpeed = 2.0f;

    [Header("Tilt Settings")]
    [Tooltip("Ignore tilt within this many degrees (deadzone).")]
    public float deadZoneDegrees = 3f;
    [Tooltip("The tilt (in degrees) that produces full strafe speed.")]
    public float maxTiltDegrees = 20f;
    [Tooltip("Smooths strafe input 0=snappy, higher = smoother.")]
    public float inputSmoothing = 8f;

    [Header("Direction")]
    [Tooltip("Move forward relative to head orientation (projected on ground). If off, uses world forward (=Z).")]
    public bool forwardFollowsHead = true;

    [Header("Quality of Life")]
    [Tooltip("Auto-calibrate your neutral (comfortable) head tilt on Start.")]
    public bool autoCalibrateOnStart = true;

    // internal state
    float _neutralRollDeg = 0f;      // baseline roll offset in degrees
    float _strafeInputSmoothed = 0f; // -1..1 after smoothing

    void Start()
    {
        if (!head)
        {
            // try to find the main camera automatically
            Camera cam = Camera.main;
            if (cam) head = cam.transform;
        }

        if (autoCalibrateOnStart) CalibrateNeutralTilt();
    }

    void Update()
    {
        if (!head) return;

        // 1) Read current head roll (ear-to-shoulder), normalized to -180..+180
        float rawRoll = NormalizeAngle(head.localEulerAngles.z);

        // 2) Subtract baseline so your comfortable head angle is "neutral"
        float rollFromNeutral = rawRoll - _neutralRollDeg;

        // 3) Convert roll into strafe input in range -1..1 with a deadzone
        float strafeInput = RollToInput(rollFromNeutral, deadZoneDegrees, maxTiltDegrees);

        // 4) Smooth input (optional for comfort)
        _strafeInputSmoothed = Mathf.Lerp(_strafeInputSmoothed, strafeInput, 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime));

        // 5) Compute movement vectors (XZ only — no gravity in this simple runner)
        Vector3 forwardDir = forwardFollowsHead ? Flatten(head.forward).normalized
                                                : Vector3.forward; // world Z

        Vector3 rightDir = Vector3.Cross(Vector3.up, Vector3.Cross(forwardDir, Vector3.up)).normalized;

        Vector3 forwardMove = forwardDir * forwardSpeed;
        Vector3 strafeMove = rightDir * (_strafeInputSmoothed * maxStrafeSpeed);

        Vector3 velocity = forwardMove + strafeMove; // m/s

        // 6) Apply movement in world space
        transform.position += velocity * Time.deltaTime;
    }

    // --- Public: call this from a UI button or debug key if you want re-calibration on demand
    public void CalibrateNeutralTilt()
    {
        if (!head) return;
        _neutralRollDeg = NormalizeAngle(head.localEulerAngles.z);
    }

    // --- Helpers ---
    static float NormalizeAngle(float degrees0to360)
    {
        float a = degrees0to360;
        if (a > 180f) a -= 360f;
        return a; // now -180..+180
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static float RollToInput(float rollDeg, float deadZoneDeg, float maxDeg)
    {
        // Deadzone
        if (Mathf.Abs(rollDeg) <= deadZoneDeg) return 0f;

        // Remove deadzone, then scale to -1..1 by maxDeg
        float sign = Mathf.Sign(rollDeg);
        float mag = Mathf.Clamp01((Mathf.Abs(rollDeg) - deadZoneDeg) / Mathf.Max(0.0001f, (maxDeg - deadZoneDeg)));
        return sign * mag;
    }
}
