using System;
using UnityEngine;
using HoloAutopsy.ColorTunnel;

namespace UnityVolumeRendering
{
    [ExecuteInEditMode]
    public class VolumeRenderedObject : MonoBehaviour
    {

        [HideInInspector]
        public TransferFunction transferFunction;

        [HideInInspector]
        public TransferFunction2D transferFunction2D;

        [HideInInspector]
        public VolumeDataset dataset;

        [HideInInspector]
        public MeshRenderer meshRenderer;

        [HideInInspector]
        [SerializeField]
        private RenderMode renderMode;
        [HideInInspector]
        [SerializeField]
        private TFRenderMode tfRenderMode;

        [HideInInspector]
        [SerializeField]
        private bool lightingEnabled;
        [HideInInspector]
        [SerializeField]
        private bool advancedLightingEnabled;
        [HideInInspector]
        [SerializeField]
        private bool cutShapeEnabled;
        [HideInInspector]
        [SerializeField]
        private bool cutShapeSemiTransparentEnabled;
        [HideInInspector]
        [SerializeField]
        private bool brushEnabled;

        [HideInInspector]
        [SerializeField]
        private Vector2 visibilityWindow = new Vector2(0.0f, 1.0f);

        [HideInInspector]
        [SerializeField]
        private bool rayTerminationEnabled = true;
        [HideInInspector]
        [SerializeField]
        private bool dvrBackward = false;

        private RenderTexture maskTex;
        private bool isMaskInitialized;
        //used for persistent colour tunnelling compute shader but not other shaders
        //private RenderTexture maskDistTex; 
        //private RenderTexture maskGradTex;

        //beta
        [SerializeField, HideInInspector]
        private bool colorTunneling = false;
        [SerializeField, HideInInspector]
        private bool persistentColorTunneling = false;
        [SerializeField, HideInInspector]
        private PersistColorTunnelRunner persistentColorTunnelingRunner = null;
        [SerializeField, HideInInspector]
        private Vector2 colorTunnelRange = new Vector2(0.5f, 1.0f);
        [SerializeField, HideInInspector]
        private Vector3 colorTunnelLocCenter = Vector3.zero;
        [SerializeField, HideInInspector]
        private float colorTunnelRadius = 0.8f;

        public SlicingPlane CreateSlicingPlane()
        {
            GameObject sliceRenderingPlane = GameObject.Instantiate(Resources.Load<GameObject>("SlicingPlane"));
            sliceRenderingPlane.transform.parent = transform;
            sliceRenderingPlane.transform.localPosition = Vector3.zero;
            sliceRenderingPlane.transform.localRotation = Quaternion.identity;
            sliceRenderingPlane.transform.localScale = Vector3.one * 0.1f; // TODO: Change the plane mesh instead and use Vector3.one
            MeshRenderer sliceMeshRend = sliceRenderingPlane.GetComponent<MeshRenderer>();
            sliceMeshRend.material = new Material(sliceMeshRend.sharedMaterial);
            Material sliceMat = sliceRenderingPlane.GetComponent<MeshRenderer>().sharedMaterial;
            sliceMat.SetTexture("_DataTex", dataset.GetDataTexture());
            sliceMat.SetTexture("_TFTex", transferFunction.GetTexture());
            sliceMat.SetMatrix("_parentInverseMat", transform.worldToLocalMatrix);
            sliceMat.SetMatrix("_planeMat", Matrix4x4.TRS(sliceRenderingPlane.transform.position, sliceRenderingPlane.transform.rotation, transform.lossyScale)); // TODO: allow changing scale

            return sliceRenderingPlane.GetComponent<SlicingPlane>();
        }

        public void SetRenderMode(RenderMode mode)
        {
            if (renderMode != mode)
            {
                renderMode = mode;
                SetVisibilityWindow(0.0f, 1.0f); // reset visibility window
            }
            UpdateMaterialProperties();
        }

        public void SetTransferFunctionMode(TFRenderMode mode)
        {
            tfRenderMode = mode;
            if (tfRenderMode == TFRenderMode.TF1D && transferFunction != null)
                transferFunction.GenerateTexture();
            else if (transferFunction2D != null)
                transferFunction2D.GenerateTexture();
            UpdateMaterialProperties();
        }

        public TFRenderMode GetTransferFunctionMode()
        {
            return tfRenderMode;
        }

        public RenderMode GetRenderMode()
        {
            return renderMode;
        }

        public bool GetLightingEnabled()
        {
            return lightingEnabled;
        }

        public void SetLightingEnabled(bool enable)
        {
            if (enable != lightingEnabled)
            {
                lightingEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public bool GetAdvancedLightingEnabled()
        {
            return advancedLightingEnabled;
        }

        public bool GetCutShapeEnabled()
        {
            return cutShapeEnabled;
        }
        public void SetCutShapeEnabled(bool enable)
        {
            if (enable != cutShapeEnabled)
            {
                cutShapeEnabled = enable;
                UpdateMaterialProperties();
            }
        }
        public bool GetCutShapeSemiTransparentEnabled()
        {
            return cutShapeSemiTransparentEnabled;
        }
        public void SetCutShapeSemiTransparentEnabled(bool enable)
        {
            if (!cutShapeEnabled) enable = false;
            if (cutShapeSemiTransparentEnabled != enable)
            {
                cutShapeSemiTransparentEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public bool GetEraserEnabled()
        {
            return brushEnabled;
        }
        public void SetEraserEnabled(bool enable)
        {
            if (enable != brushEnabled)
            {
                brushEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public void SetAdvancedLightingEnabled(bool enable)
        {
            if (advancedLightingEnabled != enable)
            {
                advancedLightingEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public void ToggleCutShapeEnabled()
        {
            SetCutShapeEnabled(!cutShapeEnabled);
        }

        public void ToggleBrush()
        {
            SetEraserEnabled(!brushEnabled);
        }

        public void SetVisibilityWindow(float min, float max)
        {
            SetVisibilityWindow(new Vector2(min, max));
        }

        public void SetVisibilityWindow(Vector2 window)
        {
            if (window != visibilityWindow)
            {
                visibilityWindow = window;
                UpdateMaterialProperties();
            }
        }

        public Vector2 GetVisibilityWindow()
        {
            return visibilityWindow;
        }

        public bool GetRayTerminationEnabled()
        {
            return rayTerminationEnabled;
        }

        public void SetRayTerminationEnabled(bool enable)
        {
            if (enable != rayTerminationEnabled)
            {
                rayTerminationEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public bool GetDVRBackwardEnabled()
        {
            return dvrBackward;
        }

        public void SetDVRBackwardEnabled(bool enable)
        {
            if (enable != dvrBackward)
            {
                dvrBackward = enable;
                UpdateMaterialProperties();
            }
        }

        /// ///////////////////////////////////////////////////////////

        public Vector2 GetColorTunnelRange()
        {
            return colorTunnelRange;
        }
        public void SetColorTunnelRange(float min, float max)
        {
            if (min != colorTunnelRange.x || max != colorTunnelRange.y)
            {
                colorTunnelRange = new Vector2(min, max);
                meshRenderer.sharedMaterial.SetFloat("_MinColorTunnelVal", colorTunnelRange.x);
                meshRenderer.sharedMaterial.SetFloat("_MaxColorTunnelVal", colorTunnelRange.y);
                //UpdateMaterialProperties();
            }
        }
        public Vector3 GetColorTunnelLocCenter()
        {
            return colorTunnelLocCenter;
        }
        public bool GetColorTunnelingEnabled()
        {
            return colorTunneling;
        }
        public void SetColorTunnelingEnabled(bool enable)
        {
            if (enable != colorTunneling)
            {
                colorTunneling = enable;
                UpdateMaterialProperties();
            }
        }

        public bool GetPersistColorTunnelingEnabled()
        {
            return persistentColorTunneling;
        }
        public void SetPersistColorTunnelingEnabled(bool enable)
        {
            if (enable != persistentColorTunneling)
            {
                persistentColorTunneling = enable;
                UpdateMaterialProperties();
            }
        }

        public PersistColorTunnelRunner GetPersistColorTunnelRunner()
        {
            return persistentColorTunnelingRunner;
        }
        public void SetPersistColorTunnelRunner(PersistColorTunnelRunner obj)
        {
            this.persistentColorTunnelingRunner = obj;
        }

        public void ToggleColorTunnelingEnabled()
        {
            SetColorTunnelingEnabled(!GetColorTunnelingEnabled());
            if (!GetColorTunnelingEnabled()) SetPersistColorTunnelingEnabled(false);
            //Temporary
            if (GetColorTunnelingEnabled()) SetPersistColorTunnelingEnabled(true);
        }
        public float GetColorTunnelRadius()
        {
            return colorTunnelRadius;
        }
        public void SetColorTunnelRadius(float radius)
        {
            if (colorTunnelRadius != radius)
            {
                colorTunnelRadius = radius;
                meshRenderer.sharedMaterial.SetFloat("_ColorTunnelRadius", colorTunnelRadius);
                //UpdateMaterialProperties();
            }
        }
        public void SetColorTunnelCenter(Vector3 centerWPos)
        {
            colorTunnelLocCenter = this.transform.worldToLocalMatrix.MultiplyPoint(centerWPos) + new Vector3(0.5f, 0.5f, 0.5f);
            meshRenderer.sharedMaterial.SetVector("_ColorTunnelLocCenter", colorTunnelLocCenter);
        }


        /// ///////////////////


        public void SetLightPosition(Vector3 pos)
        {
            Vector3 newPos = transform.InverseTransformPoint(pos);
            meshRenderer.sharedMaterial.SetVector("_LightPos", new Vector4(newPos.x + 0.5f, newPos.y + 0.5f, newPos.z + 0.5f, 0.0f));
        }
        public void SetLightLookDirection(Vector3 direction)
        {
            Vector3 newDir = transform.localToWorldMatrix.transpose.MultiplyVector(direction);
            meshRenderer.sharedMaterial.SetVector("_LightLookDir", new Vector4(newDir.x, newDir.y, newDir.z, 0.0f));
        }

        public RenderTexture GetMaskTexture()
        {
            return this.maskTex;
        }

        public bool IsMaskInitialized()
        {
            return isMaskInitialized;
        }

        public void SetMaskInitialized(bool initialized)
        {
            isMaskInitialized = initialized;
        }
        //public RenderTexture GetMaskDistTexture()
        //{
        //    return maskDistTex;
        //}
        //public RenderTexture GetMaskGradTexture()
        //{
        //    return this.maskGradTex;
        //}
        public void MakeMaskTextureNull()
        {
            if (this.maskTex != null)
            {
                this.maskTex.DiscardContents();
                this.maskTex.Release();
                this.maskTex = null;
            }

            //if (this.maskDistTex != null)
            //{
            //    this.maskDistTex.DiscardContents();
            //    this.maskDistTex.Release();
            //    this.maskDistTex = null;
            //}
            //if (this.maskGradTex != null)
            //{
            //    this.maskGradTex.DiscardContents();
            //    this.maskGradTex.Release();
            //    this.maskGradTex = null;
            //}

            UpdateMaterialProperties();
        }
        private RenderTexture CreateNewMaskTex(int dimension)
        {
            RenderTexture maskTex;
            if (dimension <= 1)
            {
                maskTex = new RenderTexture(dataset.GetDataTexture().width, dataset.GetDataTexture().height, 0, RenderTextureFormat.RFloat);
            }
            else if(dimension == 2)
            {
                maskTex = new RenderTexture(dataset.GetDataTexture().width, dataset.GetDataTexture().height, 0, RenderTextureFormat.RFloat);
                //maskDistTex = new RenderTexture(dataset.GetDataTexture().width, dataset.GetDataTexture().height, 0, RenderTextureFormat.RFloat);
            }
            else
            {
                maskTex = new RenderTexture(dataset.GetDataTexture().width, dataset.GetDataTexture().height, 0, RenderTextureFormat.ARGBHalf);
            }
            //Mask Data
            maskTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            maskTex.volumeDepth = dataset.GetDataTexture().depth;
            maskTex.enableRandomWrite = true;
            maskTex.filterMode = FilterMode.Trilinear;
            maskTex.Create();
            isMaskInitialized = false;
            ////Distance (auxiliary texture)
            //maskDistTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            //maskDistTex.volumeDepth = dataset.GetDataTexture().depth;
            //maskDistTex.enableRandomWrite = true;
            //maskDistTex.filterMode = FilterMode.Trilinear;
            //maskDistTex.Create();

            ////Mask Gradient
            //this.maskGradTex = new RenderTexture(dataset.GetDataTexture().width, dataset.GetDataTexture().height, 0, RenderTextureFormat.ARGBFloat);
            //this.maskGradTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            //this.maskGradTex.volumeDepth = dataset.GetDataTexture().depth;
            //this.maskGradTex.enableRandomWrite = true;
            //this.maskGradTex.filterMode = FilterMode.Trilinear;
            //this.maskGradTex.Create();

            return maskTex;
        }



        //Functions
        private void UpdateMaterialProperties()
        {
            if (!PickNewVolumeIfMeshRendererNull()) return;
            bool useGradientTexture = tfRenderMode == TFRenderMode.TF2D || renderMode == RenderMode.IsosurfaceRendering || lightingEnabled;
            if (!persistentColorTunneling) meshRenderer.sharedMaterial.SetTexture("_GradientTex", useGradientTexture ? dataset.GetGradientTexture() : null);

            if (tfRenderMode == TFRenderMode.TF2D)
            {
                meshRenderer.sharedMaterial.SetTexture("_TFTex", transferFunction2D.GetTexture());
                meshRenderer.sharedMaterial.EnableKeyword("TF2D_ON");
            }
            else
            {
                meshRenderer.sharedMaterial.SetTexture("_TFTex", transferFunction.GetTexture());
                meshRenderer.sharedMaterial.DisableKeyword("TF2D_ON");
            }

            if (lightingEnabled)
                meshRenderer.sharedMaterial.EnableKeyword("LIGHTING_ON");
            else
                meshRenderer.sharedMaterial.DisableKeyword("LIGHTING_ON");

            if (advancedLightingEnabled)
                meshRenderer.sharedMaterial.EnableKeyword("ADV_LIGHTING_ON");
            else
                meshRenderer.sharedMaterial.DisableKeyword("ADV_LIGHTING_ON");
            if (cutShapeEnabled)
            {
                meshRenderer.sharedMaterial.EnableKeyword("CUTSHAPE_ON");
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("CUTSHAPE_ON");
            }
            if (cutShapeSemiTransparentEnabled && cutShapeEnabled)
            {
                meshRenderer.sharedMaterial.EnableKeyword("CUTSHAPE_SEMITRANS");
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("CUTSHAPE_SEMITRANS");
            }

            if (brushEnabled)
            {
                meshRenderer.sharedMaterial.EnableKeyword("ERASER_ON");
                if (maskTex !=null && maskTex.format != RenderTextureFormat.RFloat)
                {
                    Debug.Log("Wrong texture format (" + maskTex.format + ") for brushing tool, re-generating new one!");
                    MakeMaskTextureNull();
                }
                if (maskTex == null)
                {
                    maskTex = CreateNewMaskTex(1);
                }
                meshRenderer.sharedMaterial.SetTexture("_MaskTex", maskTex);
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("ERASER_ON");
            }
            switch (renderMode)
            {
                case RenderMode.DirectVolumeRendering:
                    {
                        meshRenderer.sharedMaterial.EnableKeyword("MODE_DVR");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");
                        break;
                    }
                case RenderMode.MaximumIntensityProjectipon:
                    {
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_DVR");
                        meshRenderer.sharedMaterial.EnableKeyword("MODE_MIP");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");
                        break;
                    }
                case RenderMode.IsosurfaceRendering:
                    {
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_DVR");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
                        meshRenderer.sharedMaterial.EnableKeyword("MODE_SURF");
                        break;
                    }
            }

            meshRenderer.sharedMaterial.SetFloat("_MinVal", visibilityWindow.x);
            meshRenderer.sharedMaterial.SetFloat("_MaxVal", visibilityWindow.y);

            if (rayTerminationEnabled)
            {
                meshRenderer.sharedMaterial.EnableKeyword("RAY_TERMINATE_ON");
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("RAY_TERMINATE_ON");
            }

            if (dvrBackward)
            {
                meshRenderer.sharedMaterial.EnableKeyword("DVR_BACKWARD_ON");
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("DVR_BACKWARD_ON");
            }
            if (colorTunneling)
            {
                meshRenderer.sharedMaterial.EnableKeyword("COLOR_TUNNEL_ON");
                meshRenderer.sharedMaterial.SetFloat("_ColorTunnelRadius", colorTunnelRadius);
                meshRenderer.sharedMaterial.SetFloat("_MinColorTunnelVal", colorTunnelRange.x);
                meshRenderer.sharedMaterial.SetFloat("_MaxColorTunnelVal", colorTunnelRange.y);
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("COLOR_TUNNEL_ON");
            }
            if (persistentColorTunneling)
            {
                meshRenderer.sharedMaterial.EnableKeyword("PERSIST_COLOR_TUNNEL_ON");
                if (maskTex != null && maskTex.format != RenderTextureFormat.RFloat)//ARGBHalf)
                {
                    Debug.Log("Wrong texture format ("+maskTex.format+") for persistent colour tunnelling, re-generating new one!");
                    MakeMaskTextureNull();
                }
                if (maskTex == null)
                {
                    maskTex = CreateNewMaskTex(2);
                }
                meshRenderer.sharedMaterial.SetTexture("_MaskTex", maskTex);
                //meshRenderer.sharedMaterial.SetTexture("_GradientTex", useGradientTexture ? maskGradTex : null);
            }
            else
            {
                meshRenderer.sharedMaterial.DisableKeyword("PERSIST_COLOR_TUNNEL_ON");
            }
        }

        private bool PickNewVolumeIfMeshRendererNull()
        {
            if (meshRenderer == null)
            {
                var comps = this.gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mr in comps)
                {
                    if (mr.gameObject.name.ToLower().Contains("volumecontainer"))
                    {
                        meshRenderer = mr;
                        var par = mr.GetComponentInParent<VolumeRenderedObject>();
                        if (par != null) dataset = par.dataset;

                        MakeMaskTextureNull();
                        mr.transform.parent = this.transform;
                        if (par != null) par.transform.parent = null;

                        return true;
                    }
                }
                return false;
            }
            return true;
        }


        private void Start()
        {
            UpdateMaterialProperties();
        }
    }
}
