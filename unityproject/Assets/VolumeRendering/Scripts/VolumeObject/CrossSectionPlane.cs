using UnityEngine;

namespace UnityVolumeRendering
{
    /// <summary>
    /// Cross section plane.
    /// Used for cutting a model (cross section view).
    /// </summary>
    [ExecuteInEditMode]
    public class CrossSectionPlane : MonoBehaviour
    {
        /// <summary>
        /// Volume dataset to cross section.
        /// </summary>
        public VolumeRenderedObject targetObject;
        //Added code
        [SerializeField]
        private Transform holoVolume;

        private void OnDisable()
        {
            if (targetObject != null)
                targetObject.meshRenderer.sharedMaterial.DisableKeyword("CUTOUT_PLANE");
        }

        private void Update()
        {
            if (targetObject == null || holoVolume==null)
                return;

            Material mat = targetObject.meshRenderer.sharedMaterial;

            mat.EnableKeyword("CUTOUT_PLANE");
            mat.SetMatrix("_CrossSectionMatrix", transform.worldToLocalMatrix * holoVolume.transform.localToWorldMatrix);//targetObject.transform.localToWorldMatrix);
        }
    }
}
