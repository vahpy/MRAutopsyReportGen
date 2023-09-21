using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartVisibilityManager : MonoBehaviour
{
    [SerializeField, Range(0, 10)]
    private float noActivationTimeThresholdHead = 3.0f;
    [SerializeField, Range(0.05f, 3)]
    private float maxDistThresholdHead = 1f;

    [SerializeField, Range(0, 10)]
    private float noActivationTimeThresholdHands = 3.0f;
    [SerializeField, Range(0.05f, 1)]
    private float maxDistThresholdHands = 0.2f;

    [SerializeField]
    private List<Transform> headApproxSensObjs = new List<Transform>();

    [SerializeField]
    private List<Transform> handsApproxSensObjs = new List<Transform>();

    private List<float> lastActivationHeadObjs;
    private List<bool> defaultVisibilityHeadObjs;

    private List<float> lastActivationHandsObjs;
    private List<bool> defaultVisibilityHandsObjs;

    private bool isHeadObjsActivationUpdated;
    private bool isHandsObjsActivationUpdated;

    // Start is called before the first frame update
    void Start()
    {
        float time = Time.realtimeSinceStartup;

        lastActivationHandsObjs = new List<float>();
        lastActivationHeadObjs = new List<float>();
        defaultVisibilityHeadObjs = new List<bool>();
        defaultVisibilityHandsObjs = new List<bool>();

        foreach (Transform t in headApproxSensObjs)
        {
            var mRen = t.GetComponent<MeshRenderer>();
            if (mRen != null) defaultVisibilityHeadObjs.Add(mRen);
            else defaultVisibilityHeadObjs.Add(t.gameObject.activeSelf);
            lastActivationHeadObjs.Add(time);
        }

        foreach (Transform t in handsApproxSensObjs)
        {
            var mRen = t.GetComponent<MeshRenderer>();
            if (mRen != null) defaultVisibilityHandsObjs.Add(mRen);
            else defaultVisibilityHandsObjs.Add(t.gameObject.activeSelf);
            lastActivationHandsObjs.Add(time);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLastActivationHeadObjs();
        UpdateLastActivationHandsObjs();
        ApplyVisibilityToHeadObjs();
        ApplyVisibilityToHandsObjs();
    }

    private void ApplyVisibilityToHeadObjs()
    {
        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < lastActivationHeadObjs.Count; i++)
        {
            if (!defaultVisibilityHeadObjs[i]) continue;
            if (lastActivationHeadObjs[i] + noActivationTimeThresholdHead < time)
                SetObjVisibility(headApproxSensObjs[i], false);
            else
                SetObjVisibility(headApproxSensObjs[i], true);
        }
    }
    private void ApplyVisibilityToHandsObjs()
    {
        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < lastActivationHandsObjs.Count; i++)
        {
            if (!defaultVisibilityHandsObjs[i]) continue;
            if (lastActivationHandsObjs[i] + noActivationTimeThresholdHands < time)
                SetObjVisibility(handsApproxSensObjs[i], false);
            else
                SetObjVisibility(handsApproxSensObjs[i], true);
        }
    }
    private void SetObjVisibility(Transform t, bool visible)
    {
        if (!visible)
        {
            t.GetComponent<ObjectsVisibilityManagement>()?.ForceTurnOff(); //Disabling gameobject, stops update and invalid behaviour occurs
        }
        var mRen = t.GetComponent<MeshRenderer>();
        if (mRen != null && mRen.enabled != visible) mRen.enabled = visible;
        else if (t.gameObject.activeSelf != visible) t.gameObject.SetActive(visible);
    }
    private void UpdateLastActivationHeadObjs()
    {
        float time = Time.realtimeSinceStartup;
        Vector3 pos = Camera.main.transform.position;
        for (int i = 0; i < headApproxSensObjs.Count; i++)
        {
            if (Vector3.Distance(headApproxSensObjs[i].position, pos) < maxDistThresholdHead)
            {
                lastActivationHeadObjs[i] = time;
            }
        }
    }

    private void UpdateLastActivationHandsObjs()
    {
        float time = Time.realtimeSinceStartup;
        MixedRealityPose pose;
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out pose))
        {
            Vector3 pos = pose.Position;
            for (int i = 0; i < handsApproxSensObjs.Count; i++)
            {
                if (Vector3.Distance(handsApproxSensObjs[i].position, pos) < maxDistThresholdHands)
                {
                    lastActivationHandsObjs[i] = time;
                }
            }
        }
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out pose))
        {
            Vector3 pos = pose.Position;
            for (int i = 0; i < handsApproxSensObjs.Count; i++)
            {
                if (Vector3.Distance(handsApproxSensObjs[i].position, pos) < maxDistThresholdHands)
                {
                    lastActivationHandsObjs[i] = time;
                }
            }
        }
    }
}
