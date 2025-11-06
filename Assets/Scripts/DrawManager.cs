using UnityEngine;
using UnityEngine.InputSystem;

public class DrawManager : MonoBehaviour
{
    public LineRenderer linePrefab;       // assign a prefab with a LineRenderer
    public InputActionReference drawAction; // link to your trigger input
    public float minDistance = 0.01f;
    public LayerMask drawingSurfaceMask; // assign layer for the canvas

    private LineRenderer currentLine;
    private Vector3 lastPoint;
    private bool isDrawing;

    void Update()
    {
        bool triggerPressed = drawAction.action.ReadValue<float>() > 0.1f;

        if (triggerPressed)
        {
            RaycastHit hit;

            // Raycast from the brush tip forward
            if (Physics.Raycast(transform.position, transform.forward, out hit, 0.1f, drawingSurfaceMask))
            {
                Vector3 drawPosition = hit.point;

                transform.rotation = Quaternion.LookRotation(-hit.normal);

                if (!isDrawing)
                {
                    StartNewLine(drawPosition);
                    isDrawing = true;
                }
                else
                {
                    AddPointIfNeeded(drawPosition);
                }
            }
            else
            {
                // not touching canvas
                isDrawing = false;
                currentLine = null;
            }
        }
        else
        {
            isDrawing = false;
            currentLine = null;
        }
    }

    void StartNewLine(Vector3 startPos)
    {
        currentLine = Instantiate(linePrefab);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, startPos);
        lastPoint = startPos;
    }

    void AddPointIfNeeded(Vector3 newPos)
    {
        if (Vector3.Distance(lastPoint, newPos) > minDistance)
        {
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, newPos);
            lastPoint = newPos;
        }
    }
}
