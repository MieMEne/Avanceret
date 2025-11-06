using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MovingObstacleT : MonoBehaviour
{
    public enum Axis { X, Z }
    [Header("Motion")]
    public Axis axis = Axis.X;            // move left-right on X by default
    public float amplitude = 1.5f;        // how far from center (meters)
    public float speed = 1.2f;            // cycles per second
    public float phaseOffset = 0f;        // per-object offset (radians)

    [Header("Physics (optional)")]
    [Tooltip("If assigned, we'll move this kinematic rigidbody (recommended for moving colliders).")]
    public Rigidbody rb;

    Vector3 _start;

    void Awake()
    {
        _start = transform.position;

        // Make sure collider is solid
        var col = GetComponent<Collider>();
        col.isTrigger = false;

        // If there is a RB, make it kinematic so it moves but doesn't get pushed
        if (rb != null) rb.isKinematic = true;
    }

    void Update()
    {
        float s = Mathf.Sin((Time.time * Mathf.PI * 2f * speed) + phaseOffset) * amplitude;

        Vector3 offset = (axis == Axis.X) ? new Vector3(s, 0, 0) : new Vector3(0, 0, s);
        Vector3 target = _start + offset;

        if (rb != null) rb.MovePosition(target);
        else transform.position = target;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 a = transform.position;
        Vector3 b = a + ((axis == Axis.X) ? Vector3.right : Vector3.forward) * amplitude;
        Vector3 c = a - ((axis == Axis.X) ? Vector3.right : Vector3.forward) * amplitude;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(b, c);
        Gizmos.DrawSphere(b, 0.05f);
        Gizmos.DrawSphere(c, 0.05f);
    }
#endif
}
