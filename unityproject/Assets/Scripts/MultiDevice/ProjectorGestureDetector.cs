using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;

public class ProjectorGestureDetector : MonoBehaviour
{
    [SerializeField] private EchoServerBehavior server;
    [SerializeField] private UnityEvent<string> handGestureListner;

    //[SerializeField, Range(0f, 1f)] private float pinchStrength = 1f;
    private readonly float pinchThreshold = 0.7f; // minimum strength of detecting a pinch gesture
    public bool isRightPinching { private set; get; }
    public bool isLeftPinching { private set; get; }

    public bool isMiddleRightPinching { private set; get; }
    public bool isMiddleLeftPinching { private set; get; }

    //private MixedRealityPose pose;

    private void Awake()
    {
        if (server == null)
        {
            server = this.GetComponent<EchoServerBehavior>();
            if (server == null)
            {
                EchoServerBehavior[] servers = FindObjectsOfType<EchoServerBehavior>();
                if (servers != null && servers.Length > 0)
                {
                    server = servers[0];
                }
            }
            Debug.Log("Server Behavior Found: " + (server != null ? "Yes" : "No"));
        }

        if (handGestureListner == null)
        {
            Debug.LogWarning("Projector Hand Gesuture Listener is Null");
            handGestureListner = new UnityEvent<string>();
        }
    }

    private void Start()
    {
        isRightPinching = false;
        isLeftPinching = false;

        if(server!=null) handGestureListner.AddListener(server.SendGestureDetection);
    }


    private void Update()
    {
        // Index Finger
        if (IsPinching(Handedness.Left))
        {
            if (!isLeftPinching)
            {
                handGestureListner?.Invoke("leftpinchstart");
            }
            isLeftPinching = true;
        }
        else
        {
            if (isLeftPinching)
            {
                handGestureListner?.Invoke("leftpinchfinish");
            }
            isLeftPinching = false;
        }
        if (IsPinching(Handedness.Right))
        {
            if (!isRightPinching)
            {
                handGestureListner?.Invoke("rightpinchstart");
            }
            isRightPinching = true;
        }
        else
        {
            if (isRightPinching)
            {
                handGestureListner?.Invoke("rightpinchfinish");
            }
            isRightPinching = false;
        }

        // Middle Finger
        if (IsMiddleFingerPinching(Handedness.Left))
        {
            if (!isMiddleLeftPinching)
            {
                handGestureListner?.Invoke("leftmidpinchstart");
            }
            isMiddleLeftPinching = true;
        }
        else
        {
            if (isMiddleLeftPinching)
            {
                handGestureListner?.Invoke("leftmidpinchfinish");
            }
            isMiddleLeftPinching = false;
        }
        if (IsMiddleFingerPinching(Handedness.Right))
        {
            if (!isMiddleRightPinching)
            {
                handGestureListner?.Invoke("rightmidpinchstart");
            }
            isMiddleRightPinching = true;
        }
        else
        {
            if (isMiddleRightPinching)
            {
                handGestureListner?.Invoke("rightmidpinchfinish");
            }
            isMiddleRightPinching = false;
        }
    }

    public bool IsPinching(Handedness handSide)
    {
        if(HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handSide, out var pose))
        {
            return HandPoseUtils.CalculateIndexPinch(handSide) > pinchThreshold;
        }
        return false;
    }
    public bool IsMiddleFingerPinching(Handedness handSide)
    {
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handSide, out var pose))
        {
            return CalculateMiddlePinch(handSide) > pinchThreshold;
        }
        return false;
    }

    //Copied from MRTK HandPoseUtils for index, altered for 
    private const float MiddleThumbSqrMagnitudeThreshold = 0.0016f;
    private static float CalculateMiddlePinch(Handedness handedness)
    {
        HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, handedness, out var middlePose);
        HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out var thumbPose);

        Vector3 distanceVector = middlePose.Position - thumbPose.Position;
        float middleThumbSqrMagnitude = distanceVector.sqrMagnitude;

        float pinchStrength = Mathf.Clamp(1 - middleThumbSqrMagnitude / MiddleThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
        return pinchStrength;
    }
}
