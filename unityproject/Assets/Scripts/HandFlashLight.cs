using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityVolumeRendering;

namespace HoloAuopsy
{
    //[ExecuteInEditMode]
    public class HandFlashLight : MonoBehaviour
    {
        [SerializeField]
        private VolumeRenderedObject volObj = default;
        [SerializeField]
        private Transform pivot = default;



        private Transform flashLightObj = default;
        private float rotationSpeed = 70f;
        private bool hover = false;
        //[SerializeField]
        //private Transform lightObj = default;
        //[SerializeField]
        //private Transform arrow = default;

        private MixedRealityPose pose;

        void Start()
        {
            if (flashLightObj == null)
            {
                flashLightObj = this.GetComponent<Transform>();
            }
        }

        void Update()
        {
            if (flashLightObj != null)
            {
                if (volObj.GetAdvancedLightingEnabled())
                {
                    if (flashLightObj.hasChanged)
                    {
                        flashLightObj.hasChanged = false;
                        volObj.SetLightPosition(flashLightObj.position);
                        volObj.SetLightLookDirection(-flashLightObj.up);
                    }
                }
                else
                {
                    //Animation
                    if (hover)
                    {
                        flashLightObj.rotation = Quaternion.Euler(-90, 0, 0);
                        flashLightObj.position = pivot.position + new Vector3(0, 0.1f, 0);
                    }
                    else
                    {
                        flashLightObj.Rotate(pivot.forward, rotationSpeed * Time.unscaledDeltaTime);
                        flashLightObj.position = pivot.position + new Vector3(0, 0.1f, 0);
                    }
                }
            }
            else
            {
                if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left, out pose))
                {
                    if (!volObj.GetAdvancedLightingEnabled()) volObj.SetAdvancedLightingEnabled(true);
                    volObj.SetLightPosition(pose.Position);
                    volObj.SetLightLookDirection(-pose.Up);
                }
                else
                {
                    if (volObj.GetAdvancedLightingEnabled()) volObj.SetAdvancedLightingEnabled(false);
                }
            }
        }
        public void GrabbedFlashLightStart(ManipulationEventData data)
        {
            volObj.SetAdvancedLightingEnabled(true);
        }
        public void GrabbedFlashLightEnd(ManipulationEventData data)
        {
            if (Vector3.Distance(flashLightObj.localPosition, pivot.localPosition) < 0.2)
            {
                volObj.SetAdvancedLightingEnabled(false);
                flashLightObj.hasChanged = true;
            }
        }
        public void HoverFlashLightStart(ManipulationEventData data)
        {
            hover = true;
        }
        public void HoverFlashLightEnd(ManipulationEventData data)
        {
            hover = false;
        }
    }
}