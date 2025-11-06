using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class CrystalCollectible : MonoBehaviour
{
    [Tooltip("How much this crystal adds to the player's score.")]
    public int value = 1;

    private XRGrabInteractable grab;
    private bool collected = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Prevent double collection
        if (collected) return;
        collected = true;

        // Add the value to the collector
        if (CrystalCollector.Instance != null)
        {
            CrystalCollector.Instance.Add(value);
        }
        else
        {
            Debug.LogWarning("No CrystalCollector found in the scene!");
        }

        // Optional feedback: sound or particles
        // AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // Remove crystal from world
        Destroy(gameObject);
    }
}
