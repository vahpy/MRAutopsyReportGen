using HoloAutopsy.CuttingShape;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.CuttingShape
{
    [ExecuteInEditMode]
    public class CuttingShapeAddVertexUI : MonoBehaviour
    {
        [SerializeField]
        private MeshManipulator cuttingShape;
        [SerializeField]
        private HoloAutopsy.Utils.CustomisedHandGesture handPosLocator;
        [SerializeField]
        private Transform projectedPosLocator;
        [SerializeField]
        private LineRenderer line;

        private bool handDetected = true;
        void Update()
        {
            if (cuttingShape.IsChangingVerticesState)
            {
                handPosLocator.EnableTracking(true);
            }
            else
            {
                handPosLocator.EnableTracking(false);
            }
            if (cuttingShape.IsChangingVerticesState && handDetected)
            {
                int faceNum;
                var projPos = MeshUtils.GetProjectedPointPosition(cuttingShape.transform, handPosLocator.transform.position, out faceNum);

                if (faceNum >= 0)
                {
                    if (!projectedPosLocator.GetComponent<MeshRenderer>().enabled)
                    {
                        projectedPosLocator.GetComponent<MeshRenderer>().enabled = true;
                        if (line != null)
                        {
                            line.enabled = true;
                            line.positionCount = 2;
                            line.useWorldSpace = true;
                        }
                    }
                    if (projectedPosLocator != null) projectedPosLocator.position = projPos;
                    if (line != null) line.SetPositions(new Vector3[] { handPosLocator.transform.position, projPos });
                }
                else
                {
                    if (projectedPosLocator.GetComponent<MeshRenderer>().enabled)
                    {
                        projectedPosLocator.GetComponent<MeshRenderer>().enabled = false;
                        if (line != null) line.enabled = false;
                    }
                }
            }
            else
            {
                if (handPosLocator != null && projectedPosLocator.GetComponent<MeshRenderer>().enabled)
                {
                    projectedPosLocator.GetComponent<MeshRenderer>().enabled = false;
                    line.GetComponent<LineRenderer>().enabled = false;
                }
            }
        }

        public void HandDetected()
        {
            handDetected = true;
        }
        public void HandNotDetected()
        {
            handDetected = false;
        }
    }
}