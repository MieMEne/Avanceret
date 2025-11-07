using UnityEngine;
using UnityEngine.InputSystem;
using Oculus.Haptics;

public class DrawManager : MonoBehaviour
{
    [Header("Drawing")]
    public LineRenderer linePrefab;
    public InputActionReference drawAction;
    public float minDistance = 0.01f;
    public AudioSource drawSound;

    [Header("Haptics (Oculus)")]
    public HapticClip drawHapticClip; // assign a .haptic file from Oculus/Haptics/
    public float amplitude = 1.0f;
    public bool loopHaptics = true;
    public bool useRightHand = true;

    private LineRenderer currentLine;
    private Vector3 lastPoint;
    private bool isDrawing = false;

    private HapticClipPlayer hapticPlayer;
     
     void Awake()
    {
        if (drawHapticClip != null)
        {
            hapticPlayer = new HapticClipPlayer(drawHapticClip);
        }
    }

    void Update()
    {
        float triggerValue = drawAction.action.ReadValue<float>();
        if (triggerValue > 0.1f)
        {
            if (!isDrawing)
            {
                StartNewLine();
                isDrawing = true;

                if (drawSound != null && !drawSound.isPlaying)
                    drawSound.Play();

                if (hapticPlayer != null)
                {
                    hapticPlayer.amplitude = amplitude;

                    // Choose which controller (Right or Left)
                    var controller = useRightHand ? Controller.Right : Controller.Left;

                    // Start playing the clip
                    hapticPlayer.Play(controller);
                }
            }
            else
            {
                AddPointIfNeeded();
            }
        }
        else
        {
            if (isDrawing)
            {
                currentLine = null;
                isDrawing = false;

                if (drawSound != null && drawSound.isPlaying)
                    drawSound.Stop();

                if (hapticPlayer != null)
                {
                    hapticPlayer.Stop();
                }
            }
        }
    }

    void StartNewLine()
    {
        currentLine = Instantiate(linePrefab);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, transform.position);
        lastPoint = transform.position;
    }

    void AddPointIfNeeded()
    {
        if (Vector3.Distance(lastPoint, transform.position) > minDistance)
        {
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, transform.position);
            lastPoint = transform.position;
        }
    }
}
