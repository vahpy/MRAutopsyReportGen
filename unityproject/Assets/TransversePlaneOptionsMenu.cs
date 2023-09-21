using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;

namespace HoloAutopsy
{
    public class TransversePlaneOptionsMenu : MonoBehaviour
    {
        [SerializeField]
        private SlicingPlane transversePlane = default;
        [SerializeField]
        private SlicingPlane coronalPlane = default;
        [SerializeField]
        private SlicingPlane sagittalPlane = default;
        [SerializeField]
        private ViewModel forMainCursorControl = default;
        [SerializeField]
        private VolumeRenderedObject volRenObj;
        [SerializeField]
        private GameObject measurementTool;
        [SerializeField]
        private List<GameObject> indicatorPlates;
        public void XrayColorBtn()
        {
            transversePlane.ChangeDisplayType();
            coronalPlane.ChangeDisplayType();
            sagittalPlane.ChangeDisplayType();
        }
        public void OpaqueTransparentBtn()
        {
            forMainCursorControl.TransparentOrCT();
        }
        private void Update()
        {
            if (volRenObj != null)
            {
                indicatorPlates[1].SetActive(volRenObj.GetCutShapeEnabled());
                indicatorPlates[2].SetActive(volRenObj.GetEraserEnabled());
                indicatorPlates[7].SetActive(volRenObj.GetColorTunnelingEnabled() || volRenObj.GetPersistColorTunnelingEnabled());
            }
            if(measurementTool != null)
            {
                indicatorPlates[3].SetActive(measurementTool.GetComponent<Measurement.MeasurmentToolkit>().GetDrawingState());
            }
        }
    }
}