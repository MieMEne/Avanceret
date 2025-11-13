using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        // Decide if this instance is "wrong"
        bool wrong;
        if (detectWrongByTag)
            wrong = CompareTag(wrongTag);
        else
            wrong = isWrongCrystal; // manual checkbox (or ignore both and just set value negative)

        // Compute delta:
        // - If you want to control purely by 'value', just set value = 1 or -1 and leave detectWrongByTag=false, isWrongCrystal=false.
        // - If using tag/checkbox, we force sign based on that and use |value| as magnitude.
        int delta;
        if (detectWrongByTag || isWrongCrystal)
        {
            int mag = Mathf.Abs(value);
            delta = wrong ? -mag : mag;
        }
        else
        {
            delta = value; // simplest mode: value determines add/subtract
        }

        // Add the value to the collector
        if (CrystalCollector.Instance != null)
        {
            CrystalCollector.Instance.Add(delta);
        }
        else
        {
            Debug.LogWarning("No CrystalCollector found in the scene!");
        }

        // Feedback
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

        // Remove crystal from world
        Destroy(gameObject);
    }
}
