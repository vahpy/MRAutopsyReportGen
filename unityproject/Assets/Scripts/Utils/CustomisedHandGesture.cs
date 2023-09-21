using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace HoloAutopsy.Utils
{
    //[ExecuteInEditMode]
    public class CustomisedHandGesture : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent indexPinchAction;
        [SerializeField]
        private UnityEvent indexPinchRelease;
        [SerializeField]
        private UnityEvent middlePinchAction;
        [SerializeField]
        private UnityEvent middlePinchRelease;
        [SerializeField]
        private UnityEvent ringPinchAction;
        [SerializeField]
        private UnityEvent ringPinchRelease;
        [SerializeField]
        private UnityEvent handNotDetected;
        [SerializeField]
        private UnityEvent handDetected;
        [SerializeField]
        private Handedness trackedHand;
        [SerializeField, Range(0.01f, 0.04f)]
        private float jointThreshold = 0.020f;
        [SerializeField, Range(0.01f, 0.04f)]
        private float releaseThreshold = 0.03f;
        [SerializeField, Range(0.10f, 2f)]
        private float minChangeStateTimeThreshold = 0.5f;
        [SerializeField]
        private bool followNewPos = true;


        [SerializeField]
        private bool tempClick = false;
        private bool last_TempClick = false;

        private bool lastIndexPinchActioned;
        private bool lastMiddlePinchActioned;
        private bool lastRingPinchActioned;


        enum HandDetectionState
        {
            Detected, NotDetected, NotChecked
        };

        private HandDetectionState lastHandDetectionState;
        [SerializeField]
        private bool tracking = false;

        private float lastReleaseTapTime = 0;
        private float lastClickTapTime = 0;


        private Vector3? iTPos; //index tip 
        private Vector3? mTPos; //middle tip
        private Vector3? rTPos; //ring tip
        private Vector3? tTPos; //thumb tip


        void Start()
        {
            lastHandDetectionState = HandDetectionState.NotChecked;
            last_TempClick = tempClick;
        }

        void OnEnable()
        {
            lastIndexPinchActioned = false;
            lastMiddlePinchActioned = false;
            lastRingPinchActioned = false;
        }

        void Update()
        {
            if (!tracking) return;
            if (!Application.isPlaying)
            {
                if (last_TempClick != tempClick)
                {
                    last_TempClick = tempClick;
                    indexPinchAction?.Invoke();
                }
                return;
            }
            MixedRealityPose pose;
            iTPos = null;
            mTPos = null;
            rTPos = null;
            tTPos = null;
            if (indexPinchAction != null && HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, trackedHand, out pose))
            {
                iTPos = pose.Position;
            }
            if (middlePinchAction != null && HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, trackedHand, out pose))
            {
                mTPos = pose.Position;
            }
            if (ringPinchAction != null && HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, trackedHand, out pose))
            {
                rTPos = pose.Position;
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, trackedHand, out pose))
            {
                tTPos = pose.Position;
            }

            if (tTPos == null)
            {
                if (iTPos != null || mTPos != null || rTPos != null)
                {
                    if (followNewPos)
                    {
                        if (iTPos != null) this.transform.position = (Vector3)(iTPos);
                        if (mTPos != null) this.transform.position = (Vector3)(mTPos);
                        if (rTPos != null) this.transform.position = (Vector3)(rTPos);
                    }
                    if (lastHandDetectionState != HandDetectionState.Detected)
                    {
                        handDetected?.Invoke();
                        lastHandDetectionState = HandDetectionState.Detected;
                    }
                }
                else
                {
                    if (lastHandDetectionState != HandDetectionState.NotDetected)
                    {
                        handNotDetected?.Invoke();
                        lastHandDetectionState = HandDetectionState.NotDetected;
                    }
                }
                return;
            }
            else
            {
                // tTPos not null
                float indexDist = float.MaxValue, middleDist = float.MaxValue, ringDist = float.MaxValue;

                if (iTPos != null)
                {
                    indexDist = Vector3.Distance((Vector3)iTPos, (Vector3)tTPos);
                }
                if (mTPos != null)
                {
                    middleDist = Vector3.Distance((Vector3)mTPos, (Vector3)tTPos);
                }
                if (rTPos != null)
                {
                    ringDist = Vector3.Distance((Vector3)rTPos, (Vector3)tTPos);
                }

                if (indexDist <= middleDist && indexDist <= ringDist)
                {
                    // trigger index pinch action
                    if (followNewPos) this.transform.position = (Vector3)((tTPos + iTPos) / 2);
                    if (lastHandDetectionState != HandDetectionState.Detected)
                    {
                        handDetected?.Invoke();
                        lastHandDetectionState = HandDetectionState.Detected;
                    }
                    if (ClickDetected(indexDist) && CheckStateTransitionValidForClick() && !lastIndexPinchActioned)
                    {
                        lastIndexPinchActioned = true;
                        //Debug.Log("Index Pinch Clicked");
                        indexPinchAction?.Invoke();
                    }
                    else if (ReleaseDetected(indexDist) && lastIndexPinchActioned/* && CheckStateTransitionValidForRelease()*/)
                    {
                        lastIndexPinchActioned = false;
                        //Debug.Log("Index Pinch Released");
                        indexPinchRelease?.Invoke();
                    }
                }
                else if (middleDist <= indexDist && middleDist <= ringDist)
                {
                    // trigger middle pinch action
                    if (followNewPos) this.transform.position = (Vector3)((tTPos + mTPos) / 2);
                    if (lastHandDetectionState != HandDetectionState.Detected)
                    {
                        handDetected?.Invoke();
                        lastHandDetectionState = HandDetectionState.Detected;
                    }
                    if (ClickDetected(middleDist) && CheckStateTransitionValidForClick() && !lastMiddlePinchActioned)
                    {
                        lastMiddlePinchActioned = true;
                        middlePinchAction?.Invoke();
                    }
                    else if (ReleaseDetected(middleDist) && lastMiddlePinchActioned)
                    {
                        lastMiddlePinchActioned = false;
                        middlePinchRelease?.Invoke();
                    }
                }
                else if (ringDist != float.MaxValue)
                {
                    // trigger ring pinch action
                    if (followNewPos) this.transform.position = (Vector3)((tTPos + rTPos) / 2);
                    if (lastHandDetectionState != HandDetectionState.Detected)
                    {
                        handDetected?.Invoke();
                        lastHandDetectionState = HandDetectionState.Detected;
                    }
                    if (ClickDetected(ringDist) && CheckStateTransitionValidForClick() && !lastRingPinchActioned)
                    {
                        lastRingPinchActioned = true;
                        ringPinchAction?.Invoke();
                    }
                    else if (ReleaseDetected(ringDist) && lastRingPinchActioned)
                    {
                        lastRingPinchActioned = false;
                        ringPinchRelease?.Invoke();
                    }
                }
                else
                {
                    // Just Thumb Detected
                    if (followNewPos) this.transform.position = (Vector3)(tTPos);
                    if (lastHandDetectionState != HandDetectionState.Detected)
                    {
                        handDetected?.Invoke();
                        lastHandDetectionState = HandDetectionState.Detected;
                    }
                }
            }
        }

        public void EnableTracking(bool enable)
        {
            if (this.tracking != enable)
            {
                this.tracking = enable;
                lastHandDetectionState = HandDetectionState.NotChecked;
                lastReleaseTapTime = Time.realtimeSinceStartup;
            }
        }

        private bool ClickDetected(float dist)
        {
            if (dist <= jointThreshold)
                return true;

            return false;
        }
        //not used yet
        private bool ReleaseDetected(float dist)
        {
            if (dist >= releaseThreshold)
            {
                return true;
            }
            return false;
        }

        private bool CheckStateTransitionValidForClick()
        {
            if ((Time.realtimeSinceStartup - lastClickTapTime) > minChangeStateTimeThreshold)
            {
                lastClickTapTime = Time.realtimeSinceStartup;
                return true;
            }
            lastReleaseTapTime = Time.realtimeSinceStartup;
            return false;
        }
        private bool CheckStateTransitionValidForRelease()
        {
            if ((Time.realtimeSinceStartup - lastReleaseTapTime) > minChangeStateTimeThreshold)
            {
                lastReleaseTapTime = Time.realtimeSinceStartup;
                return true;
            }
            lastClickTapTime = Time.realtimeSinceStartup;
            return false;
        }
    }
}