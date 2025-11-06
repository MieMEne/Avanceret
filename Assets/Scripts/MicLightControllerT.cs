using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class MicLightControllerT : MonoBehaviour
{
    [Header("Which microphone to use")]
    [Tooltip("Matches part of the microphone name. For Quest PC Link use 'oculus'.")]
    public string preferredDeviceSubstring = "oculus";

    [Header("Light to control (Directional Light)")]
    public Light targetLight;

    [Header("Brightness Mapping")]
    public float minIntensity = 0f;
    public float maxIntensity = 3.5f;
    public float riseLerp = 12f;
    public float fallLerp = 6f;

    [Header("Mic Sensitivity (dB)")]
    [Tooltip("Silence / background noise level")]
    public float dbFloor = -50f;
    [Tooltip("Full yelling / max brightness level")]
    public float dbCeil = -18f;

    [Header("Mic Settings")]
    public int sampleRate = 22050;
    public int sampleCount = 512;

    private AudioSource _src;
    private AudioClip _micClip;
    private string _deviceName = null;
    private float[] _buffer;
    private float _currentIntensity;
    private bool _micReady = false;
    private int _lastLevel = -1;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.loop = true;
        _src.mute = true;
        _src.playOnAwake = false;

        _buffer = new float[Mathf.Max(64, sampleCount)];

        if (targetLight)
        {
            targetLight.intensity = minIntensity;
            _currentIntensity = minIntensity;
        }
    }

    void OnEnable()
    {
        StartCoroutine(StartMic());
    }

    IEnumerator StartMic()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone devices found.");
            yield break;
        }

        Debug.Log("Detected microphones: " + string.Join(", ", Microphone.devices));

        // Select device
        _deviceName = Microphone.devices.FirstOrDefault(d =>
            d.ToLower().Contains(preferredDeviceSubstring.ToLower())
        );

        if (string.IsNullOrEmpty(_deviceName))
            _deviceName = Microphone.devices[0];

        Debug.Log($"Using microphone: {_deviceName}");

        // Start recording
        _micClip = Microphone.Start(_deviceName, true, 1, sampleRate);
        while (Microphone.GetPosition(_deviceName) <= 0) yield return null;

        _src.clip = _micClip;
        _src.Play();
        _micReady = true;
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(_deviceName) && Microphone.IsRecording(_deviceName))
            Microphone.End(_deviceName);
    }

    void Update()
    {
        if (!_micReady || _micClip == null || targetLight == null) return;

        int pos = Microphone.GetPosition(_deviceName);
        if (pos <= 0) return;

        int start = pos - sampleCount;
        if (start < 0) start += _micClip.samples;

        _micClip.GetData(_buffer, Mathf.Max(start, 0));

        // Compute RMS → dB
        float rms = Mathf.Sqrt(_buffer.Average(s => s * s) + 1e-12f);
        float db = 20f * Mathf.Log10(rms + 1e-12f);

        // Convert dB to 0..1 brightness factor
        float t = Mathf.Clamp01(Mathf.InverseLerp(dbFloor, dbCeil, db));

        // Smooth intensity change
        float target = Mathf.Lerp(minIntensity, maxIntensity, t);
        float lerpSpeed = (target > _currentIntensity ? riseLerp : fallLerp);
        _currentIntensity = Mathf.Lerp(_currentIntensity, target, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        targetLight.intensity = _currentIntensity;

        // Convert to level 1–10
        int level = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 10f, t)), 1, 10);
        if (level != _lastLevel)
        {
            _lastLevel = level;
            Debug.Log($"Mic level: {level}/10  (dB {db:F1})   device='{_deviceName}'");
        }
    }
}
