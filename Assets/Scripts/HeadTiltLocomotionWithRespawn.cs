using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit; // for XRBaseController haptics
using Oculus.Haptics;                     // Meta Haptics SDK

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

    [Header("Bounds Respawn (walk off floor)")]
    [Tooltip("If true, player will respawn when they leave the floor plane bounds or fall below Min Y.")]
    public bool enableBoundsRespawn = true;

    [Tooltip("The floor plane. Bounds are auto-computed from its MeshRenderer.")]
    public Transform floorPlane;

    [Tooltip("Extra padding outside the plane bounds before we respawn (in meters).")]
    public float boundsPadding = 0.5f;

    [Tooltip("Computed X range of the floor (world space).")]
    public Vector2 xBounds = new Vector2(-50f, 50f);

    [Tooltip("Computed Z range of the floor (world space).")]
    public Vector2 zBounds = new Vector2(-50f, 50f);

    [Tooltip("If player Y falls below this height, they respawn.")]
    public float minY = -5f;

    [Header("Haptics On Hit (legacy XR)")]
    public bool hapticsOnHit = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.6f;
    public float hapticDuration = 0.12f;
    public float hapticRetriggerCooldown = 0.2f;

    [Tooltip("Preferred: your Action-based Left/Right controllers (XRBaseController).")]
    public XRBaseController leftController;
    public XRBaseController rightController;

    [Header("Sound On Hit")]
    [Tooltip("Assign an AudioSource with your 'error' clip (Play On Awake OFF).")]
    public AudioSource hitAudio;

    // Meta Haptics (.haptic file support)
    [Header("Meta Haptics (.haptic)")]
    [Tooltip("If assigned, this .haptic clip will be played on hit (Meta Haptics SDK).")]
    public HapticClip obstacleHitClip;          // drag & drop error-126627.haptic here
    [Tooltip("If enabled and a clip is assigned, use Meta Haptics instead of legacy XR haptics.")]
    public bool useMetaHaptics = true;

    [Range(0f, 1f)]
    [Tooltip("Scales the strength of the .haptic clip (0 = off, 1 = full).")]
    public float metaHapticsIntensity = 1f;

    [Header("Debug")]
    public bool printRollInConsole = false;

    CharacterController _cc;
    float _neutralRollDeg = 0f;
    float _strafeInputSmoothed = 0f;
    Vector3 _startPos;
    Quaternion _startRot;
    float _nextHapticTime = 0f;

    // Meta Haptics runtime player
    HapticClipPlayer _hitClipPlayer;

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

        // Auto-compute floor bounds if we have a plane
        if (enableBoundsRespawn && floorPlane != null)
        {
            var mr = floorPlane.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Bounds b = mr.bounds;
                xBounds = new Vector2(b.min.x - boundsPadding, b.max.x + boundsPadding);
                zBounds = new Vector2(b.min.z - boundsPadding, b.max.z + boundsPadding);

                // Set a reasonable minY just below the floor if current value is far away
                float suggestedMinY = b.min.y - 2f;
                if (minY < suggestedMinY - 0.01f || minY > suggestedMinY + 0.01f)
                    minY = suggestedMinY;

                if (printRollInConsole)
                    Debug.Log($"[HeadTilt] Floor bounds from '{floorPlane.name}': X {xBounds.x:F1}..{xBounds.y:F1}, Z {zBounds.x:F1}..{zBounds.y:F1}, minY {minY:F1}");
            }
            else if (printRollInConsole)
            {
                Debug.LogWarning("[HeadTilt] floorPlane has no MeshRenderer, cannot compute bounds.");
            }
        }

        CalibrateNeutralTilt();
    }

    void OnDestroy()
    {
        // clean up Meta Haptics player
        if (_hitClipPlayer != null)
        {
            _hitClipPlayer.Dispose();
            _hitClipPlayer = null;
        }
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

        // 5) Check bounds: if we walked off the floor, respawn
        if (enableBoundsRespawn)
        {
            Vector3 p = transform.position;

            bool outOfX = p.x < xBounds.x || p.x > xBounds.y;
            bool outOfZ = p.z < zBounds.x || p.z > zBounds.y;
            bool outOfY = p.y < minY;

            if (outOfX || outOfZ || outOfY)
            {
                Respawn();
                return;
            }
        }

#if UNITY_EDITOR
        // Handy editor key: press R to recalibrate neutral
        if (Input.GetKeyDown(KeyCode.R)) CalibrateNeutralTilt();
#endif
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider != null && (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("MovingObstacle")))
        {
            TriggerHaptics();           // use Meta Haptics if set

            if (hitAudio && hitAudio.clip != null)
            {
                Debug.Log("[HeadTilt] Playing hit audio: " + hitAudio.clip.name);
                hitAudio.PlayOneShot(hitAudio.clip);
            }
            else
            {
                Debug.LogWarning("[HeadTilt] No hitAudio or no clip assigned.");
            }

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
        // 1) Meta Haptics (.haptic)
        if (useMetaHaptics && obstacleHitClip != null)
        {
            if (_hitClipPlayer != null)
            {
                _hitClipPlayer.Dispose();
                _hitClipPlayer = null;
            }

            _hitClipPlayer = new HapticClipPlayer(obstacleHitClip);
            _hitClipPlayer.amplitude = Mathf.Clamp01(metaHapticsIntensity);
            _hitClipPlayer.Play(Controller.Both);
            return; // skip legacy haptics
        }

        // 2) Legacy XR haptics
        if (!hapticsOnHit) return;
        if (Time.time < _nextHapticTime) return;
        _nextHapticTime = Time.time + hapticRetriggerCooldown;

        bool usedXRI = false;

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
