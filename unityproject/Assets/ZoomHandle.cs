using HoloAutopsy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.Sliding
{
    [ExecuteInEditMode]
    public class ZoomHandle : MonoBehaviour
    {

        [SerializeField]
        private SlicingPlane plane = default;
        [SerializeField]
        private Transform shaft = default;
        [SerializeField]
        private float minZoomScale = 0.03f;
        [SerializeField,Tooltip("Set a negative number for set automatically.")]
        private float maxZoomScale = float.MinValue;


        //constants
        private readonly float handleMaxLength = 2;
        private readonly float zoomSpeed = 0.2f;
        //fields
        public bool isGrabbed = false;
        private float defaultPosX = -2;

        private void OnEnable()
        {
            transform.localPosition = new Vector3(defaultPosX, 0, 0);
            if (maxZoomScale < 0 || maxZoomScale>=1) maxZoomScale = plane.transform.localScale.x;
            UpdateProcedure();
        }
        // Update is called once per frame
        private void Update()
        {
            if (!isGrabbed) return;
            UpdateProcedure();
        }

        private void UpdateProcedure()
        {
            //stick to x-axis in boundary and no rotation
            transform.localPosition = new Vector3(Mathf.Clamp(transform.localPosition.x, defaultPosX - handleMaxLength, defaultPosX + handleMaxLength), 0, 0);
            transform.localRotation = Quaternion.identity;
            UpdateShaftPosScale();
            //Zoom
            float newXScale = plane.transform.localScale.x;
            float k = Mathf.Abs(defaultPosX - transform.localScale.x) / (2 * handleMaxLength);

            if (transform.localPosition.x < defaultPosX) //Zoom In
            {
                newXScale -= k * k * Time.fixedUnscaledDeltaTime * zoomSpeed;
            }
            else if (transform.localPosition.x > defaultPosX) //Zoom Out
            {
                newXScale += k * k * Time.fixedUnscaledDeltaTime * zoomSpeed;
            }
            newXScale = Mathf.Clamp(newXScale, minZoomScale, maxZoomScale);

            if (newXScale > 0)
            {
                plane.transform.localScale = new Vector3(newXScale, newXScale / plane.transform.localScale.x * plane.transform.localScale.y, plane.transform.localScale.z);
            }
            else
            {
                Debug.LogError("Error in implementation. Shouldn't have negative scale!");
            }
        }

        /// <summary>
        /// Update shaft position and scale to fill the gap between zoom handler and centeric sphere
        /// </summary>
        private void UpdateShaftPosScale()
        {
            shaft.localPosition = new Vector3(transform.localPosition.x / 2, 0, 0);
            shaft.localScale = new Vector3(0.2f, transform.localPosition.x / 2, 0.2f);
        }
        #region PUBLIC_API
        //Interaction Listeners
        public void StartGrab()
        {
            isGrabbed = true;
        }
        public void EndGrab()
        {
            isGrabbed = false;
            //back to default position
            var pos = transform.localPosition;
            pos.x = defaultPosX;
            transform.localPosition = pos;
            UpdateShaftPosScale();
        }
        #endregion
    }
}