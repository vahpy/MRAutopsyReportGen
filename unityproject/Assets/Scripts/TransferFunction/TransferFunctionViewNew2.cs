using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityVolumeRendering;
using MyUtils = HoloAuopsy.TransferFunctionUtils;

namespace HoloAuopsy
{
    //[ExecuteInEditMode]
    public class TransferFunctionViewNew2 : MonoBehaviour
    {
        [SerializeField] private VolumeRenderedObject volRendObject = default;
        [SerializeField] private GameObject alphaControlPointPrefab = default;
        [SerializeField] private GameObject colorControlPointPrefab = default;
        [SerializeField] private Transform histogramPlane = default;
        [SerializeField] private Transform colorBar = default;
        [SerializeField] private ControlState histControlState = ControlState.INACTIVE;

        private Texture2D histTex = null;
        private Material histPlaneMat = default;
        private Material colBarMat = default;
        private TransferFunction tf = default;
        private readonly Quaternion DEFAULT_ALPHA_CP_ROTATION = Quaternion.Euler(90, 0, 0);
        private readonly Quaternion DEFAULT_COLOR_CP_ROTATION = Quaternion.Euler(0, 0, 0);
        private float lastTouchTime;
        private bool touchReleased;

        void Start()
        {
            InitAndDrawIntensityHistogram();
            histControlState = ControlState.INACTIVE;
            lastTouchTime = 0;
            touchReleased = true;
        }

        void Update()
        {
            if (histTex == null) InitAndDrawIntensityHistogram();

            //Update Control Points and Texture
            this.UpdateCP_Texture();
        }


        //Helper Functions
        private void AlphaCPListener(ManipulationEventData data)
        {
            //Remove gameobject and control point from transfer function
            if (histControlState == ControlState.DELETE_CP)
            {
                MyUtils.RemoveAlphaControlPoint(data.ManipulationSource, tf);
            }
        }
        private void ColorCPListener(ManipulationEventData data)
        {
            //Remove gameobject and control point from transfer function
            if (histControlState == ControlState.DELETE_CP)
            {
                Debug.Log("Delee Color Control Point");
            }

        }

        private void UpdateCP_Texture()
        {
            if (histogramPlane.hasChanged)
            {
                histogramPlane.hasChanged = false;
                return;
            }
            Transform cpObj;
            bool updateTex = false;

            for (int i = 0; i < histogramPlane.childCount; i++)
            {
                cpObj = histogramPlane.GetChild(i);
                if (cpObj.hasChanged)
                {
                    cpObj.localPosition = new Vector3(Mathf.Clamp(cpObj.localPosition.x, -MyUtils.OFFSET, MyUtils.OFFSET), Mathf.Clamp(cpObj.localPosition.y, -MyUtils.OFFSET, MyUtils.OFFSET), 0);
                    cpObj.localRotation = DEFAULT_ALPHA_CP_ROTATION;

                    MyUtils.UpdateTextureByAlphaCP(i, histogramPlane, tf, false);
                    cpObj.hasChanged = false;
                    updateTex = true;
                }
            }
            for (int i = 0; i < colorBar.childCount; i++)
            {
                cpObj = colorBar.GetChild(i);
                if (cpObj.hasChanged)
                {
                    cpObj.localPosition = new Vector3(Mathf.Clamp(cpObj.localPosition.x, -MyUtils.OFFSET, MyUtils.OFFSET), 0, 0);
                    cpObj.localRotation = DEFAULT_COLOR_CP_ROTATION;

                    MyUtils.UpdateTextureByColorCP(i, colorBar, tf, false);
                    cpObj.hasChanged = false;
                    updateTex = true;
                }
            }

            if (updateTex) tf.GenerateTexture();
        }
        void InitAndDrawIntensityHistogram()
        {
            if (volRendObject == null) return;
            if ((histPlaneMat = Resources.Load<Material>("TFHistogramMat")) == null)
            {
                throw new UnassignedReferenceException("\"TFHistogramMat\" material has not been found.");
            }
            if ((colBarMat = Resources.Load<Material>("CTFunctionBarMat")) == null)
            {
                throw new UnassignedReferenceException("\"CTFunctionBarMat\" material has not been found.");
            }

            volRendObject.SetTransferFunctionMode(TFRenderMode.TF1D);
            tf = volRendObject.transferFunction;

            if (histTex == null)
            {
                if (SystemInfo.supportsComputeShaders)
                    histTex = HistogramTextureGenerator.GenerateHistogramTextureOnGPU(volRendObject.dataset);
                else
                    histTex = HistogramTextureGenerator.GenerateHistogramTexture(volRendObject.dataset);
            }
            if (histTex == null)
            {
                throw new UnassignedReferenceException("Hist Texture has not been created successfully.");
            }

            histPlaneMat.SetTexture("_HistTex", histTex);
            histPlaneMat.SetTexture("_TFTex", tf.GetTexture());
            colBarMat.SetTexture("_TFTex", tf.GetTexture());
            MyUtils.CreateAlphaControlPoints(histogramPlane, alphaControlPointPrefab, tf, AlphaCPListener);
            MyUtils.CreateColorControlPoints(colorBar, colorControlPointPrefab, tf, AlphaCPListener);
        }

        public void toggleActivation()
        {
            if (this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(false);
                histControlState = ControlState.INACTIVE;
            }
            else
            {
                this.gameObject.SetActive(true);
                this.GetComponent<Follow>().enabled = true;
                histControlState = ControlState.READY;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, true, true, true);
            }
        }

        public void ChangeToDeleteCPState()
        {
            if (this.gameObject.activeSelf)
            {
                histControlState = ControlState.DELETE_CP;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, false, false, false);
            }
        }
        public void ChangeToAddCPState()
        {
            if (this.gameObject.activeSelf)
            {
                histControlState = ControlState.ADD_CP;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, false, false, false);
                MyUtils.ChangeAddCPComponentStates(histogramPlane.gameObject, true, true,true);
            }
        }
        public void ChangeToMoveState()
        {
            if (this.gameObject.activeSelf)
            {
                histControlState = ControlState.MOVE_CP;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, false, false, false);
            }
        }

        public void BackFunc()
        {
            if (histControlState == ControlState.READY)
            {
                toggleActivation();
                histControlState = ControlState.INACTIVE;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, true, true, true);
                MyUtils.ChangeAddCPComponentStates(histogramPlane.gameObject, false, false, false);
            }
            else if (histControlState == ControlState.MOVE_CP)
            {
                histControlState = ControlState.READY;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, true, true, true);
                MyUtils.ChangeAddCPComponentStates(histogramPlane.gameObject, false, false, false);
            }
            else
            {
                histControlState = ControlState.MOVE_CP;
                MyUtils.ChangeGrabbingComponentStates(this.gameObject, false, false, false);
                MyUtils.ChangeAddCPComponentStates(histogramPlane.gameObject, false, false,false);
            }
        }

        public ControlState GetControlState()
        {
            return histControlState;
        }

        public void TouchStartedFunc(HandTrackingInputEventData data)
        {
            Debug.Log("touched!");
            if (histControlState == ControlState.ADD_CP && touchReleased && Time.realtimeSinceStartup - lastTouchTime > 2)
            {
                MixedRealityPose pose;
                if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, data.Handedness, out pose))
                {
                    Vector3 pos = histogramPlane.InverseTransformPoint(pose.Position);
                    Debug.Log("New Control Point :" + pos);
                    MyUtils.AddAlphaControlPoint(pos, histogramPlane, alphaControlPointPrefab, tf, AlphaCPListener);
                    touchReleased = false;
                }
            }
        }
        public void TouchEndedFunc(HandTrackingInputEventData data)
        {
            if (histControlState == ControlState.ADD_CP)
            {
                lastTouchTime = Time.realtimeSinceStartup;
            }
            touchReleased = true;
        }
        public void FollowBtnFunc()
        {
            if (this.GetComponent<Follow>().isActiveAndEnabled)
            {
                this.GetComponent<Follow>().enabled = false;
            }
            else
            {
                this.GetComponent<Follow>().enabled = true;
            }
        }
    }
}