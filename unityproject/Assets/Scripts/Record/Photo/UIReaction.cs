using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace HoloAutopsy.Record.Photo
{
    public class UIReaction : MonoBehaviour
    {
        [SerializeField]
        private GameObject cameraIcon = default;
        [SerializeField]
        private GameObject photoFrame = default;
        [SerializeField]
        private float rotateSpeed = 90f; //degrees per second

        private Vector3 defaultScale;
        private Quaternion defaultRotation;
        private Vector3 defaultPosition;
        private Vector3 iconScale;
        
        private float scalingSpeed;
        private float timeStart;
        private float waitTime;

        private float distanceThreshold;

        private TransitionState transitionState;
        private bool newTransit;

        enum TransitionState
        {
            Pic,
            Icon,
            PicToIcon,
            IconToPic
        }

        // Start is called before the first frame update
        void Start()
        {
            cameraIcon.SetActive(false);
            defaultScale = transform.localScale;
            defaultRotation = transform.localRotation;
            defaultPosition = transform.localPosition;
            iconScale = Vector3.one * (Vector3.Magnitude(defaultScale) * 0.1f);
            timeStart = Time.time;
            //scalingSpeed = 0.2f;
            waitTime = 3f;
            transitionState = TransitionState.Pic;
            distanceThreshold = 0.2f;
            newTransit = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (HandDistance() < distanceThreshold)
            {
                switch (transitionState)
                {
                    case TransitionState.Icon:
                        transitionState = TransitionState.Pic;
                        break;
                    case TransitionState.PicToIcon:
                        transitionState = TransitionState.Pic;
                        break;
                }
            }
            else
            {
                switch (transitionState)
                {
                    case TransitionState.Pic:
                        if (!newTransit)
                        {
                            newTransit = true;
                            timeStart = Time.time;
                        }
                        else if (Time.time - timeStart > waitTime)
                        {
                            transitionState = TransitionState.Icon;
                            newTransit = true;
                        }
                        break;
                    case TransitionState.IconToPic:
                        transitionState = TransitionState.PicToIcon;
                        break;
                }
            }
            
            // States animations
            if(transitionState == TransitionState.Pic)
            {
                photoFrame.SetActive(true);
                this.GetComponent<MeshRenderer>().enabled = true;
                cameraIcon.SetActive(false);
                this.transform.localScale = defaultScale;
                this.transform.localRotation = defaultRotation;
            }
            else if(transitionState == TransitionState.IconToPic)
            {

            }else if(transitionState == TransitionState.PicToIcon)
            {

            }
            else
            {
                cameraIcon.SetActive(true);
                this.GetComponent<MeshRenderer>().enabled = false;
                photoFrame.SetActive(false);
                this.transform.localScale = iconScale;
                if (newTransit)
                {
                    this.transform.localRotation = Quaternion.identity;
                    newTransit = false;
                }
                this.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
            }
        }


        private float HandDistance()
        {
            float distance = float.MaxValue;
            MixedRealityPose pose;
            if(HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm,Handedness.Right,out pose)) {
                distance = Vector3.Distance(transform.position, pose.Position);
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out pose))
            {
                distance = Mathf.Min(Vector3.Distance(transform.position, pose.Position), distance);
            }
            return distance;
        }
    }
}