using UnityEngine;

namespace HoloAutopsy.ColorTunnel
{
    //[ExecuteInEditMode]
    public class PersistColorTunnelRunner : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader compute;
        [SerializeField]
        private UnityVolumeRendering.VolumeRenderedObject volRenObj;
        private Transform volObj;

        private Vector3 center;
        private float radius;
        private float minVisible;
        private float maxVisible;

        private int colorTunnel_KernelID;
        private int initialize_KernelID;

        private Vector3 dataDim;
        private Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);

        private RenderTexture currentMaskTex;

        private bool activePersistent;
        private bool oneTimeForceRun;
        private void OnEnable()
        {
            InitializeComputeShader();
            activePersistent = false;
            oneTimeForceRun = false;
        }
        public void SetPersistentActive(bool active)
        {
            activePersistent = active;
        }
        public bool GetPersistentActivationState()
        {
            return activePersistent;
        }

        public void ForceRun()
        {
            oneTimeForceRun = true;
        }
        void Update()
        {
            if (volRenObj == null || !volRenObj.GetPersistColorTunnelingEnabled()) return;
            AssignMaskTex(volRenObj.GetMaskTexture());
            if (this.currentMaskTex == null) return;
            if (oneTimeForceRun || center != volRenObj.GetColorTunnelLocCenter() || radius != volRenObj.GetColorTunnelRadius()
                || minVisible != volRenObj.GetColorTunnelRange().x || maxVisible != volRenObj.GetColorTunnelRange().y)
            {
                oneTimeForceRun = false;
                center = volRenObj.GetColorTunnelLocCenter();
                radius = volRenObj.GetColorTunnelRadius();
                minVisible = volRenObj.GetColorTunnelRange().x;
                maxVisible = volRenObj.GetColorTunnelRange().y;
                RunComputeShader();
            }
        }
        private void AssignMaskTex(RenderTexture maskTex)
        {
            if (this.currentMaskTex == maskTex) return;
            this.currentMaskTex = maskTex;
            if (this.currentMaskTex != null)
            {
                InitializeComputeShader();
                //Debug.Log(volObjParent.dataset.GetDataTexture().width + "," + volObjParent.dataset.GetDataTexture().height + "," + volObjParent.dataset.GetDataTexture().depth);
                //Debug.Log(this.maskTex.width + "," + this.maskTex.height + "," + this.maskTex.volumeDepth);
            }
        }
        private void InitializeComputeShader()
        {
            if (compute == null) return;

            colorTunnel_KernelID = compute.FindKernel("CSMain");
            initialize_KernelID = compute.FindKernel("CSInitialize");

            Debug.Log("Color Tunnel KID: " + colorTunnel_KernelID);
            Debug.Log("Initialize KID: " + initialize_KernelID);

            var dataTex = volRenObj.dataset.GetDataTexture();
            currentMaskTex = volRenObj.GetMaskTexture();

            compute.SetTexture(initialize_KernelID, "DataTex", volRenObj.dataset.GetDataTexture());
            compute.SetTexture(colorTunnel_KernelID, "DataTex", volRenObj.dataset.GetDataTexture());
            //compute.SetTexture(initialize_KernelID, "GradientTex", volRenObj.dataset.GetGradientTexture());
            //compute.SetTexture(colorTunnel_KernelID, "GradientTex", volRenObj.dataset.GetGradientTexture());


            if (currentMaskTex != null)
            {
                compute.SetTexture(initialize_KernelID, "MaskTex", currentMaskTex);
                compute.SetTexture(colorTunnel_KernelID, "MaskTex", currentMaskTex);
                //compute.SetTexture(initialize_KernelID, "MaskDistTex", volRenObj.GetMaskDistTexture());
                //compute.SetTexture(colorTunnel_KernelID, "MaskDistTex", volRenObj.GetMaskDistTexture());
                //compute.SetTexture(initialize_KernelID, "MaskGradTex", volRenObj.GetMaskGradTexture());
                //compute.SetTexture(colorTunnel_KernelID, "MaskGradTex", volRenObj.GetMaskGradTexture());
            }


            volObj = volRenObj.GetComponentInChildren<Transform>();
            dataDim = new Vector3(volRenObj.dataset.dimX, volRenObj.dataset.dimY, volRenObj.dataset.dimZ);
            if (!volRenObj.IsMaskInitialized())
            {
                compute.Dispatch(initialize_KernelID, volRenObj.dataset.dimX / 8, volRenObj.dataset.dimY / 8, volRenObj.dataset.dimZ);
                volRenObj.SetMaskInitialized(true);
                Debug.Log("Mask initialized.");
            }
            else
            {
                Debug.Log("Mask already initialized.");
            }
            Debug.Log("Persistent colour tunnelling is initialised.");
        }
        private void RunComputeShader()
        {
            if (!activePersistent) return;
            if (compute == null)
            {
                Debug.LogWarning("Compute shader not assigned!");
                return;
            }
            //var locCenter = volObj.worldToLocalMatrix.MultiplyPoint(transform.position);
            var texPos = volObj.worldToLocalMatrix.MultiplyPoint(transform.position);
            texPos += offset;
            texPos.Scale(dataDim);
            compute.SetVector("center", texPos);
            //calculate radius in pixel
            float radius = volRenObj.dataset.dimZ * volRenObj.GetColorTunnelRadius() * 0.5f;/* tunnelSphere.localScale.z * 2.5f; //0.2=>1 * 1/2*/

            compute.SetFloat("radius", radius);
            compute.SetFloat("minVisible", minVisible);
            compute.SetFloat("maxVisible", maxVisible);

            //if mask texture updated in background
            if (currentMaskTex != volRenObj.GetMaskTexture())
            {
                currentMaskTex = volRenObj.GetMaskTexture();
                if (currentMaskTex != null) compute.SetTexture(colorTunnel_KernelID, "Result", currentMaskTex);
                if (volRenObj.dataset.GetDataTexture() != null) compute.SetTexture(colorTunnel_KernelID, "DataTex", volRenObj.dataset.GetDataTexture());
            }

            compute.Dispatch(colorTunnel_KernelID, volRenObj.dataset.dimX / 8, volRenObj.dataset.dimY / 8, volRenObj.dataset.dimZ);
            //Debug.Log("Dispatched - " + texPos + /* " =loc=> " + locCenter +*/ " " + radius + " " + minVisible + " < " + maxVisible);
        }

    }
}