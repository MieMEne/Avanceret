using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class DrawManager : MonoBehaviour
{
    public LineRenderer linePrefab;       // assign a prefab with a LineRenderer
    public InputActionReference drawAction; // link to your trigger input
    public float minDistance = 0.01f;

    public AudioSource drawSound; // assign your AudioSource with the MP3

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
