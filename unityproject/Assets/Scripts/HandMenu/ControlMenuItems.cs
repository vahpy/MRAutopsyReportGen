using HoloAutopsy.Record;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace HoloAuopsy
{
    public class ControlMenuItems : MonoBehaviour
    {
        [SerializeField] private ButtonConfigHelper firstBtn = default;
        [SerializeField] private ButtonConfigHelper secondBtn = default;
        [SerializeField] private ButtonConfigHelper thirdBtn = default;
        [SerializeField] private TransferFunctionViewNew2 tfView = default;
        [SerializeField] private RecordingBubbleManager rbManager = default;
        [SerializeField] private RecordedFileManager recFileManager = default;
        [SerializeField] private Transform iconHandler;
        [SerializeField] private Texture2D[] icons = default;
        
        private ControlState lastControlState;
        MixedRealityPose pose;
        void Start()
        {
            lastControlState = ControlState.ADD_CP;
        }
        void Update()
        {
            if (lastControlState != ControlState.READY && lastControlState != ControlState.INACTIVE)
            {
                if (HandJointUtils.TryGetJointPose(Microsoft.MixedReality.Toolkit.Utilities.TrackedHandJoint.IndexTip, Handedness.Right, out pose))
                {
                    iconHandler.position = new Vector3(pose.Position.x, pose.Position.y + 0.02f, pose.Position.z);
                    var lookDirection = Camera.main.transform.position - iconHandler.position;
                    iconHandler.localRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                    iconHandler.gameObject.SetActive(true);
                }
                else
                {
                    iconHandler.gameObject.SetActive(false);
                }
            }
            if (lastControlState == tfView.GetControlState()) return;
            Debug.Log("Change State from " + lastControlState + " to " + tfView.GetControlState());
            lastControlState = tfView.GetControlState();


            firstBtn.OnClick.RemoveAllListeners();
            secondBtn.OnClick.RemoveAllListeners();
            thirdBtn.OnClick.RemoveAllListeners();

            switch (tfView.GetControlState())
            {
                case ControlState.INACTIVE:
                    //Activation
                    firstBtn.gameObject.SetActive(true);
                    secondBtn.gameObject.SetActive(false);
                    thirdBtn.gameObject.SetActive(false);
                    //Name
                    firstBtn.MainLabelText = "TF Histogram";
                    //Icon
                    firstBtn.SetQuadIcon(icons[0]);
                    //Listener
                    firstBtn.OnClick.AddListener(tfView.toggleActivation);

                    //For Record Scene and Audio
                    secondBtn.gameObject.SetActive(true);
                    thirdBtn.gameObject.SetActive(true);

                    secondBtn.MainLabelText = "Record";
                    secondBtn.SetQuadIcon(icons[6]);
                    thirdBtn.MainLabelText = "Load All Files";
                    thirdBtn.SetQuadIcon(icons[5]);

                    secondBtn.OnClick.AddListener(rbManager.RecordPressed);
                    thirdBtn.OnClick.AddListener(recFileManager.LoadAllFiles);
                    break;
                case ControlState.READY:
                    //Activation
                    firstBtn.gameObject.SetActive(true);
                    secondBtn.gameObject.SetActive(true);
                    thirdBtn.gameObject.SetActive(false);
                    //Name
                    firstBtn.MainLabelText = "Back";
                    secondBtn.MainLabelText = "Move";
                    //Icon
                    firstBtn.SetQuadIcon(icons[1]);
                    secondBtn.SetQuadIcon(icons[2]);
                    //Listener
                    firstBtn.OnClick.AddListener(tfView.BackFunc);
                    secondBtn.OnClick.AddListener(tfView.ChangeToMoveState);
                    break;
                case ControlState.MOVE_CP:
                case ControlState.ADD_CP:
                case ControlState.DELETE_CP:
                    //Activation
                    firstBtn.gameObject.SetActive(true);
                    secondBtn.gameObject.SetActive(true);
                    thirdBtn.gameObject.SetActive(true);
                    //Name
                    firstBtn.MainLabelText = "Back";
                    secondBtn.MainLabelText = "Add";
                    thirdBtn.MainLabelText = "Delete";
                    //Icon
                    firstBtn.SetQuadIcon(icons[1]);
                    secondBtn.SetQuadIcon(icons[3]);
                    thirdBtn.SetQuadIcon(icons[4]);
                    //Listener
                    firstBtn.OnClick.AddListener(tfView.BackFunc);
                    secondBtn.OnClick.AddListener(tfView.ChangeToAddCPState);
                    thirdBtn.OnClick.AddListener(tfView.ChangeToDeleteCPState);
                    break;

            }
            if (lastControlState == ControlState.MOVE_CP)
            {
                iconHandler.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", icons[2]);
                iconHandler.gameObject.SetActive(true);
            }
            else if (lastControlState == ControlState.ADD_CP)
            {
                iconHandler.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", icons[3]);
                iconHandler.gameObject.SetActive(true);
            }
            else if (lastControlState == ControlState.DELETE_CP)
            {
                iconHandler.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", icons[4]);
                iconHandler.gameObject.SetActive(true);
            }
            else
            {
                iconHandler.gameObject.SetActive(false);
            }

            firstBtn.ForceRefresh();
            secondBtn.ForceRefresh();
            thirdBtn.ForceRefresh();
        }

    }
}