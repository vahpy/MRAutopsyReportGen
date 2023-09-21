using UnityEngine;
using UnityVolumeRendering;

namespace HoloAutopsy
{
    /// <summary>
    /// Cross section plane.
    /// Used for cutting a model (cross section view).
    /// </summary>
    //[ExecuteInEditMode]
    public class CrossSection : MonoBehaviour
    {
        /// <summary>
        /// Volume dataset to cross section.
        /// </summary>
        public VolumeRenderedObject targetObject;

        void Start()
        {
            transform.hasChanged = true;
        }

        void OnDisable()
        {
            if (targetObject != null)
                targetObject.meshRenderer.sharedMaterial.DisableKeyword("SLICEPLANE_ON");
            transform.hasChanged = true;
        }

        void Update()
        {
            //if (!transform.hasChanged) return;
            // trigger slicing plane that the transform has been changed
            SlicingPlane tvSlicingPlane = transform.GetComponent<SlicingPlane>();
            //if (tvSlicingPlane != null)
            //{
            //    tvSlicingPlane.isModified = true;
            //}

            if (targetObject == null)
                return;
            Material mat = targetObject.meshRenderer.sharedMaterial;

            mat.EnableKeyword("SLICEPLANE_ON");
            //mat.SetVector("_PlanePos", targetObject.transform.position - transform.position);
            //mat.SetVector("_PlaneNormal", transform.forward);
            //transform.hasChanged = false;
        }
    }
}
