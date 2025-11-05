using UnityEngine;
using UnityEngine.InputSystem;

public class DrawManager : MonoBehaviour
{
    public LineRenderer linePrefab;       // assign a prefab with a LineRenderer
    public InputActionReference drawAction; // link to your trigger input
    public float minDistance = 0.01f;

    private LineRenderer currentLine;
    private Vector3 lastPoint;

    void Update()
    {
        // Check if drawing input is pressed
        if (drawAction.action.ReadValue<float>() > 0.1f)
        {
            if (currentLine == null)
            {
                StartNewLine();
            }
            else
            {
                AddPointIfNeeded();
            }
        }
        else
        {
            currentLine = null; // stop drawing when trigger released
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
