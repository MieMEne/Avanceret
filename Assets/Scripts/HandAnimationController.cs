using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class HandAnimationController : MonoBehaviour
{
    public InputActionProperty grabAction;
    public InputActionProperty triggerAction;

    public Animator myAnimator;

    void Update()
    {
        float grabValue = grabAction.action.ReadValue<float>();
        myAnimator.SetFloat("Grab", grabValue);

        float triggerValue = triggerAction.action.ReadValue<float>();
        myAnimator.SetFloat("Trigger", triggerValue); 
    }
}
