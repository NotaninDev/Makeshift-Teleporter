using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCollider : MonoBehaviour
{
    // mouseWasOver, mouseIsOver are true if a mouse was over the collider last frame / this frame
    // updated is true if mouseWasOver, mouseIsOver are updated this frame
    // mouseClickStartedHere is true if the mouse click started over the collider
    // mouseClickStartedHere is reset when the mouse button is released
    // buttonReleasedHere is true if the mouse button is released this frame and mouseClickStartedHere was true last frame
    private bool mouseWasOver, mouseIsOver;
    private bool mouseClickStartedHere, buttonReleasedHere;
    private bool updated;
    public Collider2D targetCollider;

    public void Initialize(Collider2D collider)
    {
        mouseWasOver = mouseIsOver= false;
        mouseClickStartedHere = buttonReleasedHere = false;
        updated = false;
        targetCollider = collider;
    }

    void Update() { if (!updated) { UpdateInternally(); } }
    void LateUpdate() { updated = false; }

    // naming like this to avoid naming this Update()
    private void UpdateInternally()
    {
        mouseWasOver = mouseIsOver;
        mouseIsOver = General.GetMouseHover(targetCollider.transform);
        if (Input.GetMouseButtonDown(0)) { mouseClickStartedHere = mouseIsOver; }
        if (Input.GetMouseButtonUp(0))
        {
            buttonReleasedHere = mouseClickStartedHere && mouseIsOver;
            mouseClickStartedHere = false;
        }
        else { buttonReleasedHere = false; }
        updated = true;
    }

    // call these functions only in Update()
    public bool GetMouseHover()
    {
        if (!updated) { UpdateInternally(); }
        return mouseIsOver;
    }
    public bool GetMouseEnter()
    {
        if (!updated) { UpdateInternally(); }
        return mouseIsOver && !mouseWasOver;
    }
    public bool GetMouseExit()
    {
        if (!updated) { UpdateInternally(); }
        return !mouseIsOver && mouseWasOver;
    }
    public bool GetMouseRelease()
    {
        if (!updated) { UpdateInternally(); }
        return buttonReleasedHere;
    }
    public bool GetMouseClick()
    {
        if (!updated) { UpdateInternally(); }
        return Input.GetMouseButtonDown(0) && mouseIsOver;
    }
}
