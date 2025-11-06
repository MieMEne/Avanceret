using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class DrawManager : MonoBehaviour
{
    public LineRenderer linePrefab;       // assign a prefab with a LineRenderer
    public InputActionReference drawAction; // link to your trigger input
    public float minDistance = 0.01f;

    public AudioSource drawSound; // assign your AudioSource with the MP3
    public HapticImpulsePlayer hapticPlayer;

    private LineRenderer currentLine;
    private Vector3 lastPoint;
    private bool isDrawing = false;

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
                    hapticPlayer.SendHapticImpulse(0.5f, 0.1f);  // amplitude, duration
            }
            else
            {
                AddPointIfNeeded();

                // optionally send repeated small haptics as you draw
                if (hapticPlayer != null)
                    hapticPlayer.SendHapticImpulse(0.2f, 0.05f);
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
