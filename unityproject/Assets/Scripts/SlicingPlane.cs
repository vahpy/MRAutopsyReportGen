using UnityEngine;
using UnityVolumeRendering;
using HoloAutopsy.Utils;
using System;
using UnityEngine.Events;
using HoloAutopsy.Record.Photo;
using HoloAutopsy.Record.Logging;

namespace HoloAutopsy
{
    [ExecuteInEditMode]
    public class SlicingPlane : MonoBehaviour
    {
        [SerializeField]
        private Transform testSphere = default;
        //public bool isModified { get; set; }
        [SerializeField]
        private Material colorfulMat = default;
        [SerializeField]
        private Material xrayMat = default;

        [SerializeField]
        private Material colorfulMat2 = default;
        [SerializeField]
        private Material xrayMat2 = default;

        [SerializeField]
        private Material crossMatRed = default;
        [SerializeField]
        private Material crossMatGreen = default;

        [SerializeField]
        private Transform virtualDisplay = default;
        [SerializeField]
        private Transform secondVirtualDisplay = default;

        [SerializeField]
        private VolumeRenderedObject holoBody = default;


        [SerializeField]
        private bool xRayEnabled = false;
        private bool _lastXRayEnabled;

        [SerializeField] private UnityEvent<byte[]> sliceImageConsumer = default;
        [SerializeField] private UnityEvent<string, string> sliceInfoConsumer = default;
        [SerializeField] private bool sendImages = default;
        public bool rotating { private set; get; }
        public bool grabbed { private set; get; }

        private CTDisplayImageExporter imageExporter = default;
        private MeshRenderer displayRenderer = default;
        private MeshRenderer secondDisplayRenderer = default;
        private bool zoomInEnabled = false;
        private bool _last_ZoomInEnabled = true;

        public bool secondDisplayOn { private set; get; }
        private bool _last_SecondDiplayOn = true;
        //Update controllers


        void OnEnable()
        {
            displayRenderer = virtualDisplay.GetComponent<MeshRenderer>();
            if (secondVirtualDisplay != null) secondDisplayRenderer = secondVirtualDisplay.GetComponent<MeshRenderer>();
            if (displayRenderer == null)
            {
                Debug.LogError("mesh renderer not found!");
            }
            xrayMat.SetTexture("_DataTex", holoBody.dataset.GetDataTexture());
            xrayMat.EnableKeyword("_DrawOutline");

            if (xrayMat2)
            {
                xrayMat2.SetTexture("_DataTex", holoBody.dataset.GetDataTexture());
                xrayMat2.DisableKeyword("_DrawOutline");
            }

            colorfulMat.SetTexture("_DataTex", holoBody.dataset.GetDataTexture());
            colorfulMat.SetTexture("_GradientTex", holoBody.dataset.GetGradientTexture());
            colorfulMat.SetTexture("_TFTex", holoBody.transferFunction2D.GetTexture());
            colorfulMat.EnableKeyword("_DrawOutline");

            if (colorfulMat2)
            {
                colorfulMat2.SetTexture("_DataTex", holoBody.dataset.GetDataTexture());
                colorfulMat2.SetTexture("_GradientTex", holoBody.dataset.GetGradientTexture());
                colorfulMat2.SetTexture("_TFTex", holoBody.transferFunction2D.GetTexture());
                colorfulMat2.DisableKeyword("_DrawOutline");
            }

            imageExporter = virtualDisplay?.GetComponentInChildren<CTDisplayImageExporter>();

            secondDisplayOn = false;
            _last_ZoomInEnabled = !zoomInEnabled;
            _last_SecondDiplayOn = !secondDisplayOn;
            _lastXRayEnabled = !xRayEnabled;
            rotating = false;
            grabbed = false;

            MainUpdateProcedure();
        }

        void Update()
        {
            UpdateDisplayMaterialType();
            UpdateSecondDisplay();
            UpdatePlaneOutline();
            if (!transform.hasChanged)
            {
                rotating = false;
                return;
            }
            rotating = true;
            transform.hasChanged = false;

            MainUpdateProcedure();
            if (sendImages) ExportImageToListeners();
        }

        private void MainUpdateProcedure()
        {
            Vector3 bScale = holoBody.transform.lossyScale; //holo `b`ody `scale`
            Vector3 pScale = transform.lossyScale;          //cutting `p`lane `scale`
            Vector3 pPos = transform.position;
            float maxScale = Mathf.Max(bScale.x, bScale.y, bScale.z);

            float minSize = Mathf.Min(bScale.x, bScale.y, bScale.z);
            if (minSize > pScale.x)
            {
                zoomInEnabled = true;
            }
            else
            {
                zoomInEnabled = false;
            }

            //Adaptive zooming
            if (zoomInEnabled)
            {
                maxScale = Mathf.Min(pScale.x, pScale.y);
            }
            else
            {
                var pointList = MathFunctions.IntersectPlaneScaleBox(holoBody.transform, transform);
                if (pointList.Count > 2)
                {
                    Vector3[] vs = MathFunctions.LongestDiagonal(pointList);
                    pPos = new Vector3((vs[0].x + vs[1].x) / 2, (vs[0].y + vs[1].y) / 2, (vs[0].z + vs[1].z) / 2);
                    maxScale = Vector3.Distance(vs[0], vs[1]);
                }
                else if (pointList != null && pointList.Count > 0)
                {
                    pPos = pointList[0];
                }
            }
            Vector3 newScale = new Vector3(maxScale, maxScale, maxScale);

            if (testSphere != null) testSphere.position = pPos;

            Vector3 eulAngles = transform.eulerAngles;
            Quaternion newRot = Quaternion.Euler(eulAngles.x, eulAngles.y, 0);

            displayRenderer.sharedMaterial.SetMatrix("_planeMat", Matrix4x4.TRS(pPos, newRot, newScale)); // TODO: allow changing scale
            displayRenderer.sharedMaterial.SetMatrix("_parentInverseMat", holoBody.transform.worldToLocalMatrix);

            if (secondDisplayOn && secondDisplayRenderer != null)
            {
                secondDisplayRenderer.sharedMaterial.SetMatrix("_planeMat", Matrix4x4.TRS(transform.position, transform.rotation, secondVirtualDisplay.lossyScale)); // TODO: allow changing scale
                secondDisplayRenderer.sharedMaterial.SetMatrix("_parentInverseMat", holoBody.transform.worldToLocalMatrix);
            }
        }

        private void UpdateDisplayMaterialType()
        {
            if (xRayEnabled == _lastXRayEnabled) return;
            _lastXRayEnabled = xRayEnabled;

            if (xRayEnabled)
            {
                displayRenderer.sharedMaterial = xrayMat;
                if (secondDisplayRenderer != null) secondDisplayRenderer.sharedMaterial = xrayMat2;
            }
            else
            {
                displayRenderer.sharedMaterial = colorfulMat;
                if (secondDisplayRenderer != null) secondDisplayRenderer.sharedMaterial = colorfulMat2;
            }
            MainUpdateProcedure();
        }

        private void UpdateSecondDisplay()
        {
            if (secondDisplayOn == _last_SecondDiplayOn) return;
            _last_SecondDiplayOn = secondDisplayOn;

            if (secondDisplayRenderer == null) return;

            if (secondDisplayOn)
            {
                secondDisplayRenderer.enabled = true;
                if (xRayEnabled)
                {
                    secondDisplayRenderer.sharedMaterial = xrayMat2;
                }
                else
                {
                    secondDisplayRenderer.sharedMaterial = colorfulMat2;
                }
            }
            else
            {
                secondDisplayRenderer.enabled = false;
                //TurnPlaneOutline(true);
            }
            MainUpdateProcedure();
        }


        private void UpdatePlaneOutline()
        {
            if (secondDisplayOn && secondDisplayRenderer != null) return;

            if (zoomInEnabled == _last_ZoomInEnabled) return;
            _last_ZoomInEnabled = zoomInEnabled;

            if (zoomInEnabled) //Zoom in enabled
            {
                transform.GetComponent<MeshRenderer>().sharedMaterial = crossMatRed;
            }
            else
            {
                transform.GetComponent<MeshRenderer>().sharedMaterial = crossMatGreen;
            }
        }
        #region PUBLIC_API
        public bool IsZoomInState()
        {
            return zoomInEnabled;
        }
        public void ChangeDisplayType()
        {
            xRayEnabled = !xRayEnabled;
            _lastXRayEnabled = !xRayEnabled;
        }
        public void GrabStart()
        {
            grabbed = true;
        }
        public void GrabEnd()
        {
            grabbed = false;
        }
        public void ChangeSecondDisplayState()
        {
            if (secondDisplayOn)
            {
                TurnSecondDisplay(false);
            }
            else
            {
                TurnSecondDisplay(true);
            }
        }
        public void TurnSecondDisplay(bool on)
        {
            secondDisplayOn = on;
            _last_SecondDiplayOn = !secondDisplayOn;
        }
        public void TurnPlaneOutline(bool on)
        {
            secondDisplayOn = !on;
            _last_SecondDiplayOn = !secondDisplayOn;
            _last_ZoomInEnabled = !zoomInEnabled;
        }
        #endregion

        // For multi-device, image exporter
        private void ExportImageToListeners()
        {
            if (imageExporter == null) return;
            byte[] data = imageExporter.GetPNGFromDisplay();
            if (data != null)
            {
                //sliceInfoConsumer?.Invoke("transform", StringUtils.TransformToString(this.transform));
                if (LoggingManager.Instance?.IsRecording == true)
                {
                    sliceInfoConsumer?.Invoke("transform&frame", StringUtils.TransformToString(this.transform) + "," + LoggingManager.Instance.frameNum + "," + LoggingManager.Instance.metaFilePath);
                }
                else
                {
                    sliceInfoConsumer?.Invoke("transform&frame", StringUtils.TransformToString(this.transform) + ",-1,");
                }
                sliceImageConsumer?.Invoke(data);
            }
        }
    }
}