using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HoloAutopsy
{
    public class BeizierSlider : MonoBehaviour
    {
        enum SliderAxis
        {
            X, Y, Z
        }

        [Tooltip("Distance between two slices in mm")]
        [SerializeField] private float dataResolution = 1;
        [SerializeField] private int numberOfPoints = 1000;
        [SerializeField] private float z_threshold = 0.1f;
        [SerializeField] private const float defaultZoomSliceSize = 0.2f;
        [SerializeField] private SliderAxis axis = SliderAxis.X;
        [Range(1, 3)] [SerializeField] private int zoomIncrease = 3;

        [SerializeField] private Transform startPoint = default;
        [SerializeField] private Transform middlePoint = default;
        [SerializeField] private Transform endPoint = default;
        [SerializeField] private Transform pivotPoint = default;
        [SerializeField] private Transform sliderLocator = default;
        [SerializeField] private Transform sliderSpace = default;

        [Header("Events")]
        [SerializeField] private UnityEvent sliderEvents = default;

        private GameObject line1;
        private GameObject line2;


        //variables
        private bool invokedEvent = false;
        private bool changeToStandardSliderAllowed = false;
        private bool backBtnToPivotPoint;

        private Vector3 endPoint_zoomed;
        private Vector3 startPoint_zoomed;

        private Vector3 firstControl_loc;
        private Vector3 secondControl_loc;
        private Transform[] points;

        private bool standardState;

        private Quaternion defaultRotation;
        private Vector3 tempVec;

        private Pose prev_MiddlePointPose;
        private Pose prev_SliderLocatorPose;

        #region PUBLIC_API
        public float GetPosition()
        {
            switch (axis)
            {
                case SliderAxis.X:
                    return sliderLocator.position.x;
                case SliderAxis.Y:
                    return sliderLocator.position.y;
                case SliderAxis.Z:
                    return sliderLocator.position.z;
            }
            return 0;
        }
        public void SetPosition(Vector3 pos)
        {
            Vector3 convertedPos = transform.worldToLocalMatrix.MultiplyPoint(pos);
            
            switch (axis)
            {
                case SliderAxis.X:
                    convertedPos.y = 0;
                    convertedPos.z = 0;
                    break;
                case SliderAxis.Y:
                    convertedPos.x = 0;
                    convertedPos.z = 0;
                    break;
                case SliderAxis.Z:
                    convertedPos.x = 0;
                    convertedPos.y = 0;
                    break;
            }
            if (Vector3.Distance(convertedPos, sliderLocator.localPosition) > 0.001)
            {
                sliderLocator.localPosition = convertedPos;
                pivotPoint.localPosition = convertedPos;
                middlePoint.localPosition = convertedPos;
            }
        }
        #endregion

        void Awake()
        {
            standardState = true;
            points = new Transform[numberOfPoints];
            //changeToStandardSliderAllowed = false;
            defaultRotation = middlePoint.localRotation;
            prev_MiddlePointPose = new Pose(middlePoint.localPosition, middlePoint.localRotation);
            prev_SliderLocatorPose = new Pose(sliderLocator.localPosition, sliderLocator.localRotation);
        }
        void Update()
        {
            //Run code if there is a change in position or rotation
            if (middlePoint.localPosition == prev_MiddlePointPose.position && middlePoint.localRotation == prev_MiddlePointPose.rotation) return;
            prev_MiddlePointPose.position = middlePoint.localPosition;
            prev_MiddlePointPose.rotation = middlePoint.localRotation;

            //Stick to y=0, z>0 (for X-axis slider) and in slider boundry
            tempVec.x = Mathf.Clamp(middlePoint.localPosition.x, startPoint.localPosition.x, endPoint.localPosition.x);
            /*if(standardState)*/
            tempVec.z = Mathf.Clamp(middlePoint.localPosition.z, 0, 1000);
            //else tempVec.z = Mathf.Clamp(middlePoint.localPosition.z, z_threshold, 1000);
            tempVec.y = 0;
            middlePoint.localRotation = defaultRotation;//middlePoint.rotation = defaultRotation;
            middlePoint.localPosition = tempVec;

            //State transfer
            if (standardState && middlePoint.localPosition.z >= z_threshold)
            {
                pivotPoint.localPosition = new Vector3(middlePoint.localPosition.x, 0, 0);
                standardState = false;
                if (line1 != null) line1.SetActive(true);
                if (line2 != null) line2.SetActive(true);
                changeToStandardSliderAllowed = false;
            }
            else if (!standardState /*&& changeToStandardSliderAllowed*/ && z_threshold > (Vector3.Distance(pivotPoint.localPosition, middlePoint.localPosition)))
            {
                standardState = true;
                middlePoint.localPosition = pivotPoint.localPosition;
                if (line1 != null) line1.SetActive(false);
                if (line2 != null) line2.SetActive(false);
            }

            //Update based on state
            if (standardState)
            {
                StateOnStandardSlider();
            }
            else
            {
                StateOnZoomedSlider();
            }
            sliderLocator.localPosition = new Vector3(TranslateToMainSliderX(middlePoint.localPosition.x), 0, 0);
            if (sliderLocator.localPosition != prev_SliderLocatorPose.position || sliderLocator.rotation != prev_SliderLocatorPose.rotation)
            {
                prev_SliderLocatorPose.position = sliderLocator.localPosition;
                prev_SliderLocatorPose.rotation = sliderLocator.rotation;
                if (sliderEvents != null)
                {
                    sliderEvents.Invoke();
                }
            }
        }

        #region standardslider
        private void StateOnStandardSlider()
        {
            calcControllerPos();
            float step = (endPoint.localPosition.x - startPoint.localPosition.x) / numberOfPoints;
            Transform point = null;
            for (int i = 0; i < numberOfPoints; i++)
            {
                point = getPointInstance(i);
                point.localPosition = calcLocationOnStandardSlider(i * step + startPoint.localPosition.x);
            }
            pivotPoint.localPosition = new Vector3(TranslateToMainSliderX(middlePoint.localPosition.x), 0, 0);
        }
        private Vector3 calcLocationOnStandardSlider(float x)
        {
            if (x <= firstControl_loc.x || x >= secondControl_loc.x) return new Vector3(x, 0, 0);
            if (x <= middlePoint.localPosition.x)
            {
                return calcNormalBeizier(x, firstControl_loc, middlePoint.localPosition);
            }
            else
            {
                return calcNormalBeizier(x, middlePoint.localPosition, secondControl_loc);
            }
        }
        private Vector3 calcNormalBeizier(float x, Vector3 p1, Vector3 p4)
        {
            float x_m = (p4.x + p1.x) / 2;
            Vector3 p2 = new Vector3(p4.x + middlePoint.localPosition.z * middlePoint.localPosition.z, p1.y, p1.z);
            Vector3 p3 = new Vector3(p1.x - middlePoint.localPosition.z * middlePoint.localPosition.z, p4.y, p4.z);
            float d = (x - p1.x) / (p4.x - p1.x);
            float d_1 = 1 - d;
            Vector3 P = d_1 * d_1 * d_1 * p1;
            P += d * d * d * p4;
            P += 3 * d_1 * d_1 * d * p2;
            P += 3 * d_1 * d * d * p3;
            return P;
        }
        private float TranslateToMainSliderX(float x_zoomSlider)
        {
            if (standardState) return x_zoomSlider;
            double x = secondControl_loc.x - firstControl_loc.x;
            x = (x_zoomSlider - startPoint_zoomed.x) * x;
            x = x / (endPoint_zoomed.x - startPoint_zoomed.x) + firstControl_loc.x;
            return (float)x;
        }
        public void SetStandardSliderAllowed()
        {
            //Debug.Log("button released!");
            //this.changeToStandardSliderAllowed = true;
        }
        #endregion

        #region zoom slider
        private void StateOnZoomedSlider()
        {
            calcControllerPos();
            calcZoomedSliderBoundries();


            //Draw two lines from the seperated part of standard slider to the zoom slider
            if (line1 == null) //similarly line2 is also Null
            {
                line1 = new GameObject("Line1");
                line1.transform.SetParent(this.transform);
                line1.transform.localPosition = Vector3.zero;
                line2 = new GameObject("Line2");
                line2.transform.SetParent(this.transform);
                line2.transform.localPosition = Vector3.zero;
            }

            //Drawline
            DrawLine(line1, firstControl_loc, startPoint_zoomed, Color.gray, Color.white, true);
            DrawLine(line2, secondControl_loc, endPoint_zoomed, Color.gray, Color.white, true);

            //Draw the zoom slider
            int startId = TranslateToArrayIndex(firstControl_loc);
            int endId = TranslateToArrayIndex(secondControl_loc);

            getPointInstance(startId).localPosition = startPoint_zoomed;
            getPointInstance(startId).name = "Start Point Zoom";
            getPointInstance(endId).localPosition = endPoint_zoomed;
            getPointInstance(endId).name = "End Point Zoom";
            Transform point, previousPoint = getPointInstance(startId);

            float step = (endPoint_zoomed.x - startPoint_zoomed.x) / (endId - startId);
            for (int i = startId + 1; i < endId; i++)
            {
                point = getPointInstance(i);
                point.localPosition = new Vector3(previousPoint.localPosition.x + step, startPoint_zoomed.y, startPoint_zoomed.z);
                previousPoint = point;
            }

            //Set position of all other dots to the standard slider (or beizier curve)
            step = (endPoint.localPosition.x - startPoint.localPosition.x) / numberOfPoints;
            for (int i = 0; i < numberOfPoints; i++)
            {
                if (i < startId || i > endId)
                {
                    getPointInstance(i).localPosition = calcLocationOnStandardSlider(i * step + startPoint.localPosition.x);
                }
            }
        }

        private void calcZoomedSliderBoundries()
        {
            float dis = Vector3.Distance(startPoint.localPosition, endPoint.localPosition);
            if (middlePoint.localPosition.z <= z_threshold)
            {
                startPoint_zoomed = new Vector3(pivotPoint.localPosition.x - dis / 2, 0, z_threshold);
                endPoint_zoomed = new Vector3(pivotPoint.localPosition.x + dis / 2, 0, z_threshold);
            }
            else
            {
                startPoint_zoomed = new Vector3(pivotPoint.localPosition.x - dis / 2, 0, middlePoint.localPosition.z);
                endPoint_zoomed = new Vector3(pivotPoint.localPosition.x + dis / 2, 0, middlePoint.localPosition.z);
            }
        }

        private float TranslateToZoomSliderX(float x_standardSlider)
        {
            if (standardState) return x_standardSlider;
            double x = endPoint_zoomed.x - startPoint_zoomed.x;
            x = (x_standardSlider - firstControl_loc.x) * x;
            x = x / (secondControl_loc.x - firstControl_loc.x) + startPoint_zoomed.x;
            return (float)x;
        }

        private Vector3 calcLocationOnZoomSlider(float x)
        {
            if (middlePoint.localPosition.z > z_threshold)
            {
                return new Vector3(x, 0, middlePoint.localPosition.z);
            }
            else
            {
                return new Vector3(x, 0, z_threshold);
            }
        }
        #endregion

        private Transform getPointInstance(int i)
        {
            if (i < 0) i = 0;
            if (i >= numberOfPoints) i = numberOfPoints - 1;

            if (points[i] == null)
            {
                points[i] = Instantiate(sliderLocator, Vector3.zero, Quaternion.identity, sliderSpace);
                points[i].localScale = new Vector3(0.002f, 0.005f, 0.01f);
                points[i].name = "point#" + i;
            }
            return points[i];
        }

        private void calcControllerPos()
        {
            if (middlePoint.localPosition.z <= z_threshold)
            {
                firstControl_loc = new Vector3(pivotPoint.localPosition.x - defaultZoomSliceSize / 2, 0, 0);
                secondControl_loc = new Vector3(pivotPoint.localPosition.x + defaultZoomSliceSize / 2, 0, 0);

            }
            else
            {
                var magnitude = z_threshold / middlePoint.localPosition.z;
                switch (zoomIncrease)
                {
                    case 1:
                        magnitude = magnitude * defaultZoomSliceSize / 2;
                        break;
                    case 2:
                        magnitude = magnitude * magnitude * defaultZoomSliceSize / 2;
                        break;
                    default:
                        magnitude = magnitude * magnitude * magnitude * defaultZoomSliceSize / 2;
                        break;
                }
                firstControl_loc = new Vector3(pivotPoint.localPosition.x - magnitude, 0, 0);
                secondControl_loc = new Vector3(pivotPoint.localPosition.x + magnitude, 0, 0);
            }
        }

        private int TranslateToArrayIndex(Vector3 point)
        {
            double id;
            if (point.z == 0)
            {
                id = point.x - startPoint.localPosition.x;
                id *= numberOfPoints;
                id /= (endPoint.localPosition.x - startPoint.localPosition.x);
                return Mathf.RoundToInt((float)id);
            }

            id = TranslateToMainSliderX(point.x) - startPoint.localPosition.x;
            id *= numberOfPoints;
            id /= (endPoint.localPosition.x - startPoint.localPosition.x);
            return Mathf.RoundToInt((float)id);
        }

        /// <summary>
        /// Draws a line on a gameobject
        /// </summary>
        /// <param name="myLine">An empty gameobject</param>
        /// <param name="start">start local or world position</param>
        /// <param name="end">end local or world position</param>
        /// <param name="localTransform">Whether the positions are local (true) or world position (false)</param>
        void DrawLine(GameObject myLine, Vector3 start, Vector3 end, Color startColor, Color endColor, bool localTransform)
        {
            LineRenderer lr = myLine.GetComponent<LineRenderer>();
            if (lr == null)
            {
                myLine.AddComponent<LineRenderer>();
                lr = myLine.GetComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Standard"));
            }
            lr.startColor = startColor;
            lr.endColor = endColor;
            lr.startWidth = 0.001f;
            lr.endWidth = 0.001f;
            if (localTransform)
            {
                lr.SetPosition(0, this.transform.TransformPoint(start));
                lr.SetPosition(1, this.transform.TransformPoint(end));
            }
            else
            {
                lr.SetPosition(0, start);
                lr.SetPosition(1, end);
            }
        }
    }
}