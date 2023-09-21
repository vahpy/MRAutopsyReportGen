using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Photon.Pun;
using UnityEngine;

public class HandTracking : MonoBehaviour/*, IMixedRealitySourceStateHandler, // Handle source detected and lost
    IMixedRealityHandJointHandler // handle joint position updates for hands*/
{
    private enum HandSide { Left, Right };
    public bool trackHand;
    [SerializeField]
    private HandSide hand = default;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    public Transform wrist = default;
    public Transform thumbFinger = default;
    public Transform indexFinger = default;
    public Transform middleFinger = default;
    public Transform ringFinger = default;
    public Transform pinkyFinger = default;


    //Global variables
    private MixedRealityPose pose;
    private Handedness handedness;

    private void Awake()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;
        handedness = (hand == HandSide.Right ? Handedness.Right : Handedness.Left);
        trackHand = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        //isMine = GetComponent<PhotonView>().IsMine;
        //if (isMine)
        //{
        //    //transform.GetChild(0).gameObject.SetActive(visibleMyHand);
        //}
        //else
        //{
        //    //transform.GetChild(0).gameObject.SetActive(visibleOthersHand);
        //    return;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (trackHand)
        {
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, handedness, out pose))
            {
                wrist.position = pose.Position;
                wrist.rotation = Quaternion.LookRotation(-pose.Up, pose.Forward);
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out pose))
            {
                thumbFinger.position = pose.Position;
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out pose))
            {
                indexFinger.position = pose.Position;
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, handedness, out pose))
            {
                middleFinger.position = pose.Position;
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, handedness, out pose))
            {
                ringFinger.position = pose.Position;
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyTip, handedness, out pose))
            {
                pinkyFinger.position = pose.Position;
            }
        }
    }

    //private void OnEnable()
    //{
    //    // Instruct Input System that we would like to receive all input events of type
    //    // IMixedRealitySourceStateHandler and IMixedRealityHandJointHandler
    //    CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
    //    CoreServices.InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);
    //}

    //private void OnDisable()
    //{
    //    // This component is being destroyed
    //    // Instruct the Input System to disregard us for input event handling
    //    CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
    //    CoreServices.InputSystem?.UnregisterHandler<IMixedRealityHandJointHandler>(this);
    //}

    //// IMixedRealitySourceStateHandler interface
    //public void OnSourceDetected(SourceStateEventData eventData)
    //{
    //    //var hand = eventData.Controller as IMixedRealityHand;

    //    //// Only react to articulated hand input sources
    //    //if (hand != null)
    //    //{
    //    //    Debug.Log("Source detected: " + hand.ControllerHandedness);
    //    //}
    //}

    //public void OnSourceLost(SourceStateEventData eventData)
    //{
    //    //var hand = eventData.Controller as IMixedRealityHand;

    //    //// Only react to articulated hand input sources
    //    //if (hand != null)
    //    //{
    //    //    Debug.Log("Source lost: " + hand.ControllerHandedness);
    //    //}
    //}

    //public void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
    //{
    //    var handness = eventData.Handedness;
    //    if ((handness.IsLeft() && this.hand == HandSide.Left) || (handness.IsRight() && this.hand == HandSide.Right))
    //    {

    //        if (eventData.InputData.TryGetValue(TrackedHandJoint.Palm, out pose))
    //        {
    //            transform.position = pose.Position;
    //            transform.rotation = pose.Rotation;
    //        }

    //    }
    //}
}
