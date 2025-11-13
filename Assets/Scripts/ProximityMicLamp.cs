using UnityEngine;

public class ProximityMicLamp : MonoBehaviour
{
    [Header("Lamp setup")]
    [Tooltip("The point/spot light that belongs to this lamp.")]
    public Light lampLight;

    [Tooltip("The player or XR Origin we measure distance from.")]
    public Transform player;

    [Header("Mic brightness source")]
    [Tooltip("The same light that MicLightControllerT is already controlling (Directional Light).")]
    public Light micDrivenLight;   // your big scene light

    [Header("How bright the mic-driven light can be")]
    [Tooltip("Directional light intensity when mic is 'silent'.")]
    public float micMinIntensity = 0f;

    [Tooltip("Directional light intensity when mic is 'very loud'.")]
    public float micMaxIntensity = 2f;

    [Header("Lamp behaviour")]
    [Tooltip("Minimum lamp intensity when completely inactive.")]
    public float lampMinIntensity = 0f;

    [Tooltip("Maximum lamp intensity when player is close and speaking loudly.")]
    public float lampMaxIntensity = 5f;

    [Tooltip("How far away (in meters) the player can be for this lamp to react.")]
    public float activationRadius = 10f;

    void Reset()
    {
        // Auto-grab a Light on this GameObject if possible
        if (!lampLight)
            lampLight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        if (!lampLight || !player || !micDrivenLight)
            return;

        // 1) How bright is the mic light right now? 0..1
        float mic01 = 0f;
        if (micMaxIntensity > micMinIntensity)
        {
            mic01 = Mathf.InverseLerp(micMinIntensity, micMaxIntensity, micDrivenLight.intensity);
        }

        // 2) How close is the player to THIS lamp? 0..1  (1 = standing next to it)
        float distance = Vector3.Distance(player.position, transform.position);
        float proximity01 = Mathf.Clamp01(1f - distance / activationRadius);

        // 3) Combine loudness + proximity
        float combined01 = mic01 * proximity01;

        // 4) Set lamp intensity
        float targetIntensity = Mathf.Lerp(lampMinIntensity, lampMaxIntensity, combined01);
        lampLight.intensity = targetIntensity;
    }
}
