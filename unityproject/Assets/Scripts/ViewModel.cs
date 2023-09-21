using HoloAutopsy.Utils;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.MultiUserCapabilities;
using UnityEngine;
using UnityVolumeRendering;

namespace HoloAutopsy
{
    [ExecuteInEditMode]
    public class ViewModel : MonoBehaviour
    {
        [SerializeField] private PhotonLobby lobby = default;
        [SerializeField] private Transform mainCursor = default;
        [SerializeField] private Transform transverseCSPlane = default;
        [SerializeField] private Transform sagittalCSPlane = default;
        [SerializeField] private Transform coronalCSPlane = default;
        [SerializeField] private BeizierSlider sliderX = default;
        [SerializeField] private Transform sliderY = default;
        [SerializeField] private Transform sliderZ = default;

        //Persistent fields during runtime
        private Vector3 _lastMainCursorPos = Vector3.zero;

        private SlicingPlane transversePlaneSlicingComponent = null;


        #region PUBLIC_API
        public void UpdateCrossSectionPlaneX() => UpdateCrossSectionPlaneByBeizierSliderX();
        public void UpdateCrossSectionPlaneX(SliderEventData data) => UpdateCrossSectionPlaneByBeizierSliderX();

        public void UpdateCrossSectionPlaneY(SliderEventData data) => UpdateCrossSectionPlanes(data, 1);

        public void UpdateCrossSectionPlaneZ(SliderEventData data) => UpdateCrossSectionPlanes(data, 2);

        public void RunRemoteMultiUser()
        {
            this.RunLocalMultiUser();
        }

        public void RunLocalMultiUser()
        {
            Debug.Log("\nActivating Photon.");
            //lobby.gameObject.SetActive(true);
        }
        #endregion

        private void OnEnable()
        {
            if (transversePlaneSlicingComponent == null)
            {
                transversePlaneSlicingComponent = transverseCSPlane.GetComponent<SlicingPlane>();
            }
        }

        private void UpdateCrossSectionPlaneByBeizierSliderX()
        {
            if (sliderX != null)
            {
                mainCursor.position = new Vector3(sliderX.GetPosition(), mainCursor.position.y, mainCursor.position.z);
            }
        }

        ///<summary>
        /// axis: 0- X , 1- Y , 2- Z
        ///</summary>
        private void UpdateCrossSectionPlanes(SliderEventData data, int axis)
        {
            if (data == null || data.Slider == null) return;
            Vector3 v;
            switch (axis)
            {
                case 0:
                    v = mainCursor.position;
                    v.x = data.Slider.ThumbRoot.transform.position.x;
                    mainCursor.position = v;
                    break;
                case 1:
                    v = mainCursor.position;
                    v.y = data.Slider.ThumbRoot.transform.position.y;
                    mainCursor.position = v;
                    break;
                case 2:
                    v = mainCursor.position;
                    v.z = data.Slider.ThumbRoot.transform.position.z;
                    mainCursor.position = v;
                    break;
            }
        }

        void Update()
        {

            if (mainCursor.position != _lastMainCursorPos)
            {
                _lastMainCursorPos = mainCursor.position;
                
                transverseCSPlane.localPosition = mainCursor.localPosition;
                coronalCSPlane.localPosition = new Vector3(0, mainCursor.localPosition.y, 0);
                sagittalCSPlane.localPosition = new Vector3(0, 0, mainCursor.localPosition.z);

                sliderX.SetPosition(mainCursor.position);
                sliderY.position = new Vector3(sliderY.position.x, coronalCSPlane.position.y, sliderY.position.z);
                sliderZ.position = new Vector3(sliderZ.position.x, sliderZ.position.y, sagittalCSPlane.position.z);
            }

            if (transversePlaneSlicingComponent.rotating)
            {
                mainCursor.rotation = transverseCSPlane.rotation;
                mainCursor.Rotate(0, 90, 0);
            }
        }

        private void MainCursorVisible(bool visible)
        {
            Renderer[] renderChildren = mainCursor.GetComponentsInChildren<Renderer>();
            int i = 0;
            for (i = 0; i < renderChildren.Length; ++i)
            {
                renderChildren[i].enabled = visible;
            }
        }
        private void ChangeToCTMaterial(bool ctEnable)
        {
            var slicingPlane = transverseCSPlane.GetComponent<SlicingPlane>();
            if (ctEnable)
            {
                slicingPlane.TurnSecondDisplay(true);
            }
            else
            {
                slicingPlane.TurnSecondDisplay(false);
            }
        }
        public void TransparentOrCT()
        {
            var slicingPlane = transverseCSPlane.GetComponent<SlicingPlane>();
            if (slicingPlane.secondDisplayOn)
            {
                MainCursorVisible(true);
                ChangeToCTMaterial(false);
            }
            else
            {
                MainCursorVisible(false);
                ChangeToCTMaterial(true);
            }
            
        }
    }
}