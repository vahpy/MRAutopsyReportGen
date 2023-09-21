using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine.Events;


namespace HoloAutopsy.CuttingShape
{
    public class ControlBrushInteraction : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent grabStart;
        [SerializeField]
        private UnityEvent grabEnd;

        [SerializeField]
        private UnityEvent indexTouched;
        [SerializeField]
        private UnityEvent middleTouched;

        void Update()
        {
            if (grabStart == null || grabEnd == null)
            {
                Debug.Log("No event listener for grab start and end!");
                return;
            }
            MixedRealityPose indexPose;
            MixedRealityPose middlePose;
            MixedRealityPose thumbPose;
            bool thumbDetected = HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out thumbPose);
            bool indexDetected = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out indexPose);
            bool middleDetected = HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out middlePose);

            if ((indexDetected && thumbDetected) || (middleDetected && thumbDetected))
            {

                //position a point between index tip and thumb tip

                this.transform.position = (indexPose.Position + thumbPose.Position) / 2;
                var indexDist = Vector3.Distance(indexPose.Position, thumbPose.Position);
                var middleDist = Vector3.Distance(middlePose.Position, thumbPose.Position);
                if (indexDist <= middleDist)
                {
                    this.transform.position = (indexPose.Position + thumbPose.Position) / 2;
                    indexTouched.Invoke();
                    if (indexDist < 0.025) grabStart.Invoke();
                    else grabEnd.Invoke();
                }
                else
                {
                    this.transform.position = (middlePose.Position + thumbPose.Position) / 2;
                    middleTouched.Invoke();
                    if (middleDist < 0.025) grabStart.Invoke();
                    else grabEnd.Invoke();
                }
            }
            else if (indexDetected)
            {
                this.transform.position = indexPose.Position;
            }
            else if (middleDetected)
            {
                this.transform.position = middlePose.Position;
            }
            else if (thumbDetected)
            {
                this.transform.position = thumbPose.Position;
            }
            else
            {
                grabEnd.Invoke();
            }
        }
    }
}