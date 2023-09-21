using HoloAutopsy.Record.Logging;
using HoloAutopsy.Record.Photo;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;


namespace HoloAutopsy.MultiDevice
{
    [RequireComponent(typeof(ProjectorGestureDetector))]
    public class CursorProjection : MonoBehaviour
    {
        [SerializeField] private Transform eyes;
        [SerializeField] private Transform fingerPointer;
        [SerializeField] private Transform cursor;
        [SerializeField] private Transform projectorPlane;
        [SerializeField] private MatchPlaneQRCode qrCodeAdjustment;
        [SerializeField] private EchoServerBehavior server;
        [SerializeField] private BoxCollider effectCubeCollider; // this is cube around the main volume, just for some visual effects
        [SerializeField] private LoggingManager loggingManager;
        [SerializeField] private ShowSavedImg imageViewerManager = default;
        [SerializeField] private PlayerController playerController = default;
        [SerializeField] private bool qrCodeAdjustmentEnabled = default;

        //[SerializeField] Transform[] landmarks;

        private ProjectorGestureDetector gestDetector;

        private Vector3 lastCursorPos = Vector3.zero;

        public bool successDraggedFromProjector { private set; get; } = false;
        private bool prevHit;
        private float joinRequestTimeStamp;
        private bool last_qrCodeAdjustmentEnabled;
        // handle receiving images
        private byte[] lastNotConsumedImgData;

        private void Awake()
        {
            gestDetector = this.GetComponent<ProjectorGestureDetector>();
            //previousLandmarksPos = new Vector3[landmarks.Length];
            //if (landmarks.Length == 3)
            //{
            //    for (int i = 0; i < landmarks.Length; i++)
            //    {
            //        previousLandmarksPos[i] = landmarks[i].position;
            //    }
            //}
            if (effectCubeCollider != null) effectCubeCollider.GetComponent<MeshRenderer>().enabled = false;
            prevHit = false;
            lastNotConsumedImgData = null;
            joinRequestTimeStamp = float.MinValue;
            last_qrCodeAdjustmentEnabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            //adjustPlaneTransform();

            if (projectorPlane == null || eyes == null || fingerPointer == null) return;

            // Check if server is connected to one client, ask for projector plane adjustment
            if (qrCodeAdjustmentEnabled != last_qrCodeAdjustmentEnabled)
            {
                last_qrCodeAdjustmentEnabled = qrCodeAdjustmentEnabled;
                qrCodeAdjustment.enabled = qrCodeAdjustmentEnabled;
            }
            if (qrCodeAdjustmentEnabled && !qrCodeAdjustment.planeAdjusted)
            {
                if (joinRequestTimeStamp + 4 < Time.realtimeSinceStartup)
                {
                    joinRequestTimeStamp = Time.realtimeSinceStartup;
                    server?.SendNewMessage("joinRequest", "");
                }
            }

            //Update finger position 
            MixedRealityPose pose;
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out pose))
            {
                fingerPointer.position = pose.Position;
            }
            else
            {
                return;
            }

            Vector3 newCursorPosition = calcCursorPosition(eyes.position, fingerPointer.position, projectorPlane);
            if (!float.IsNaN(newCursorPosition.x))
            {
                cursor.position = newCursorPosition;
            }

            //print cursor position on the plane local
            string debugString = "Pre: ";
            var localPosOfCursor = projectorPlane.worldToLocalMatrix.MultiplyPoint(newCursorPosition);
            debugString += localPosOfCursor.ToString();
            //localPosOfCursor.Scale(projectorPlane.localScale);
            localPosOfCursor *= 0.1f;
            localPosOfCursor += Vector3.one * 0.5f;
            debugString += " => " + localPosOfCursor;
            //Debug.Log(debugString);

            if (lastCursorPos != localPosOfCursor)
            {
                lastCursorPos = localPosOfCursor;
                server.SendNewMessage("movecursor", localPosOfCursor.x + "," + localPosOfCursor.y + "," + localPosOfCursor.z);

                // this if / else is for downloading and dropping object on the volume
                if (IsGrabbedObjectHitVolume(localPosOfCursor))
                {
                    if (successDraggedFromProjector)
                    {
                        if (effectCubeCollider != null) effectCubeCollider.GetComponent<MeshRenderer>().enabled = true;
                        prevHit = true;
                    }
                    else
                    {
                        if (effectCubeCollider != null) effectCubeCollider.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
                else
                {
                    if (effectCubeCollider != null) effectCubeCollider.GetComponent<MeshRenderer>().enabled = false;
                    if (prevHit && !gestDetector.isMiddleLeftPinching && !gestDetector.isMiddleRightPinching)
                    {
                        // Last frame released pinch (dropped), while ray hits the volume
                        server?.SendNewMessage("retDraggedElement", "");
                        successDraggedFromProjector = false;
                    }
                    prevHit = false;
                }
            }
        }

        //This function checks whether user is grabbing something to the MR from the Projector plane (shared board)
        public void NewTextDataFromClient(string data)
        {
            if (data.StartsWith("draggedsuccess"))
            {
                successDraggedFromProjector = true;
            }
            else if (data.StartsWith("draggedfail"))
            {
                successDraggedFromProjector = false;
            }
            else if (data.StartsWith("TransverseCSPlane"))
            {
                loggingManager.PlayOneFrame(data);
                // check if there is recording
                var tokens = data.Split(',');
                if (tokens.Length > 0 && tokens[tokens.Length - 2] != "-1")
                {
                    print(string.Join("|", tokens));
                    print(tokens[12]);
                    playerController?.PlaybackEvent(int.Parse(tokens[12]), string.Join(",", tokens, 13, tokens.Length - 13));
                }
            }
            else if (data.StartsWith("NewImagePlane"))
            {
                // instantiate new image plane
                if (lastNotConsumedImgData != null)
                {
                    GameObject viewerPlane = imageViewerManager.ShowReceivedImage(lastNotConsumedImgData);
                    if (viewerPlane != null)
                    {
                        LoggingManager.AssignTranfrom(viewerPlane, data, true);
                        var tokens = data.Split(',');
                        if (tokens.Length > 0 && tokens[tokens.Length - 2] != "-1")
                        {
                            playerController?.PlaybackEvent(int.Parse(tokens[10]), string.Join(",", tokens, 11, tokens.Length - 11));
                        }
                    }
                    else
                    {
                        print("Image drop was unsuccessful");
                    }
                }
            }
        }
        public void NewBlobDataFromClient(byte[] data)
        {
            lastNotConsumedImgData = data;
        }
        private bool IsGrabbedObjectHitVolume(Vector3 cursorPos)
        {
            if (!gestDetector.isMiddleLeftPinching && !gestDetector.isMiddleRightPinching) return false;
            if (cursorPos.x >= 0 && cursorPos.y >= 0 && cursorPos.x <= 1 && cursorPos.y <= 1) return false;
            var eyePos = eyes.transform.position;
            var fingerPos = fingerPointer.transform.position;

            RaycastHit hit;
            if (effectCubeCollider.Raycast(new Ray(eyes.transform.position, fingerPos - eyePos), out hit, 100))
            {
                return true;
            }
            return false;
        }

        private Vector3 calcCursorPosition(Vector3 eyePosition, Vector3 fingerPosition, Transform projectorPlane)
        {
            Ray ray = new Ray(eyePosition, fingerPosition - eyePosition);
            Vector3 intersectionPoint;
            // Get the normal of the plane
            Vector3 planeNormal = projectorPlane.up;

            // Get the distance of the ray origin to the plane
            float rayDistanceToPlane = Vector3.Dot(planeNormal, ray.origin - projectorPlane.position);

            // Calculate the t parameter of the intersection point
            float t = -rayDistanceToPlane / Vector3.Dot(planeNormal, ray.direction);

            // Calculate the intersection point
            intersectionPoint = ray.origin + t * ray.direction;

            // Return whether the ray intersects the plane or not
            if (t >= 0)
            {
                return intersectionPoint;
            }
            else
            {
                return new Vector3(float.NaN, float.NaN, float.NaN);
            }
        }

        //private void adjustPlaneTransform()
        //{
        //    if (landmarks == null || landmarks.Length < 3) return;
        //    //if any changed
        //    int i;
        //    for (i = 0; i < landmarks.Length; i++)
        //    {
        //        if (landmarks[i].position != previousLandmarksPos[i])
        //        {
        //            previousLandmarksPos[0] = landmarks[0].position;
        //            previousLandmarksPos[1] = landmarks[1].position;
        //            previousLandmarksPos[2] = landmarks[2].position;

        //            break;
        //        }
        //    }
        //    if (i >= 3) return;

        //    //changed, update the plane
        //    AdjustPlane(projectorPlane.gameObject, previousLandmarksPos[1], previousLandmarksPos[0], previousLandmarksPos[2]);
        //}
        //private static void AdjustPlane(GameObject plane, Vector3 point1, Vector3 point2, Vector3 point3)
        //{
        //    // Get the normal of the plane by calculating the cross product of two edges
        //    Vector3 normal = Vector3.Cross(point2 - point1, point3 - point1).normalized;
        //    plane.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);

        //    // Get the center point of the triangle formed by the three points
        //    Vector3 center = (point1 + point2) / 2;
        //    plane.transform.position = center;

        //    // Scale the plane to fit the three points
        //    float maxDistance = 0;
        //    float distance = Vector3.Distance(center, point1);
        //    if (distance > maxDistance) maxDistance = distance;
        //    distance = Vector3.Distance(center, point2);
        //    if (distance > maxDistance) maxDistance = distance;
        //    distance = Vector3.Distance(center, point3);
        //    if (distance > maxDistance) maxDistance = distance;

        //    // Calculate the distance from the third point to the line formed by the first two points
        //    Vector3 lineDirection = point2 - point1;
        //    lineDirection.Normalize();
        //    float height = Vector3.Dot(point3 - point1, lineDirection);

        //    // Set the height of the plane
        //    plane.transform.localScale = new Vector3(maxDistance * 0.1f, height * 0.05f, maxDistance * 0.1f);
        //}
    }
}