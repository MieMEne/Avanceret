using UnityEngine;

[DisallowMultipleComponent]
public class VoiceProximityTorch : MonoBehaviour
{
    [Header("Refs")]
    public Light torchLight;
    public ParticleSystem flameFX;           // optional (if your flame is particles)
    public Renderer flameRenderer;           // optional (if your flame is a mesh with emission)

    [Header("Proximity")]
    public string playerTag = "Player";
    public float triggerStayFade = 0.2f;     // fade out delay when you leave zone

    [Header("Mic")]
    public string microphoneDevice = "";     // leave empty = default mic
    [Range(8000, 48000)] public int sampleRate = 24000;
    public int micBufferSeconds = 10;

    [Header("Voice Gate (RMS)")]
    [Range(0.001f, 0.2f)] public float voiceOnThreshold = 0.035f;
    [Range(0.0005f, 0.15f)] public float voiceOffThreshold = 0.02f;
    public float minOnTime = 0.15f;
    public float silenceHold = 0.15f;

    [Header("Light Tuning")]
    public float maxIntensity = 3.5f;
    public float fadeInSeconds = 0.08f;
    public float fadeOutSeconds = 0.3f;

    [Header("Emission Color (if using mesh flame)")]
    [ColorUsage(true, true)] public Color emissionColor = new Color(1f, 0.5f, 0.1f);
    public float emissionBoost = 2.5f;

    // runtime
    private bool inZone = false;
    private float currentIntensity = 0f;
    private AudioClip micClip;
    private bool micReady = false;
    private float[] temp = new float[1024];
    private float lastAbove, lastOn;

    void Start()
    {
        if (torchLight) torchLight.intensity = 0f;
        ToggleParticles(false);
        SetEmission(0f);

        // start mic
        if (Microphone.devices.Length == 0) { Debug.LogWarning($"{name}: no mic found"); return; }
        string dev = string.IsNullOrEmpty(microphoneDevice) ? Microphone.devices[0] : microphoneDevice;
        micClip = Microphone.Start(dev, true, micBufferSeconds, sampleRate);
        StartCoroutine(WaitForMicReady(dev));
    }

    System.Collections.IEnumerator WaitForMicReady(string dev)
    {
        int pos = 0;
        while (Microphone.IsRecording(dev) && pos <= 0) { pos = Microphone.GetPosition(dev); yield return null; }
        micReady = true;
    }

    void OnDestroy() { StopMic(); }
    void OnDisable() { StopMic(); }
    void StopMic()
    {
        if (Microphone.devices.Length > 0) Microphone.End(null);
        if (micClip) Destroy(micClip);
        micClip = null; micReady = false;
    }

    void Update()
    {
        bool active = inZone; // only react in zone
        if (!active || !micReady || micClip == null) { Smooth(false); return; }

        const int window = 1024;
        int pos = Microphone.GetPosition(null);
        if (pos < window) { Smooth(false); return; }
        int start = pos - window; if (start < 0) start += micClip.samples;
        if (temp.Length != window) temp = new float[window];
        micClip.GetData(temp, start);

        double sum = 0;
        for (int i = 0; i < window; i++) { float s = temp[i]; sum += s * s; }
        float rms = Mathf.Sqrt((float)(sum / window));

        float t = Time.time;
        bool aboveOn = rms >= voiceOnThreshold;
        bool aboveOff = rms >= voiceOffThreshold;

        if (aboveOn)
        {
            lastAbove = t;
            if (currentIntensity <= 0.0001f) lastOn = t;
            Smooth(true);
        }
        else
        {
            bool canOff = (t - lastOn) >= minOnTime && (t - lastAbove) >= silenceHold && !aboveOff;
            Smooth(!canOff);
        }
    }

    void Smooth(bool on)
    {
        float target = on ? maxIntensity : 0f;
        float speed = on ? (maxIntensity / Mathf.Max(0.0001f, fadeInSeconds))
                          : (maxIntensity / Mathf.Max(0.0001f, fadeOutSeconds));

        currentIntensity = Mathf.MoveTowards(currentIntensity, target, speed * Time.deltaTime);

        if (torchLight)
        {
            torchLight.enabled = currentIntensity > 0.01f;
            torchLight.intensity = currentIntensity;
        }

        float v = Mathf.InverseLerp(0f, maxIntensity * 0.6f, currentIntensity);
        ToggleParticles(currentIntensity > 0.05f);
        SetEmission(v);
    }

    void ToggleParticles(bool on)
    {
        if (!flameFX) return;
        var em = flameFX.emission;
        em.rateOverTime = on ? 40f : 0f;
        if (on && !flameFX.isPlaying) flameFX.Play();
        if (!on && flameFX.isPlaying) flameFX.Stop();
    }

    void SetEmission(float normalized)
    {
        if (!flameRenderer) return;
        var m = flameRenderer.material;
        if (!m.IsKeywordEnabled("_EMISSION")) m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emissionColor * (normalized * emissionBoost));
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) inZone = true;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) inZone = false;
    }
}
