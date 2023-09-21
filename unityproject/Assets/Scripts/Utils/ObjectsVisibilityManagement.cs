using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsVisibilityManagement : MonoBehaviour
{
    [SerializeField]
    private List<MeshRenderer> controlledRenderers = new List<MeshRenderer>();

    private List<bool> defaultVisibility;
    private bool state; // true: on, false: off
    private bool lastState;

    private float timeFromLastAction;
    private const float timeThreshold = 3.0f;
    // Start is called before the first frame update
    private void Start()
    {
        defaultVisibility = new List<bool>();
        foreach (MeshRenderer renderer in controlledRenderers)
        {
            defaultVisibility.Add(renderer.enabled);
        }
        state = true;
        lastState = false;

        timeFromLastAction = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        //State Control
        StateControl();

        //Apply State
        if(lastState != state)
        {
            lastState = state;
            ApplyStateToControlledRenderers();
        }
    }

    public void ForceTurnOff()
    {
        state = false;
        ApplyStateToControlledRenderers();
    }

    public void ResetLastInteraction()
    {
        timeFromLastAction = Time.realtimeSinceStartup;
    }
    private void StateControl()
    {
        if(timeFromLastAction + timeThreshold < Time.realtimeSinceStartup)
        {
            state = false;
        }
        else
        {
            state = true;
        }
    }
    private void ApplyStateToControlledRenderers()
    {
        for (int i = 0; i < controlledRenderers.Count; i++)
        {
            if (!defaultVisibility[i]) continue;
            controlledRenderers[i].enabled = state;
        }
    }
}
