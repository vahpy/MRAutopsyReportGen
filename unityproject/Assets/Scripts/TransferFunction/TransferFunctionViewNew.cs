using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityVolumeRendering;

namespace HoloAuopsy
{
    [ExecuteInEditMode]
    public class TransferFunctionViewNew : MonoBehaviour
    {
        [SerializeField] private VolumeRenderedObject volRendObject = default;
        [SerializeField] private bool RefreshTextures = false;
        bool RefreshTextures_last = true;
        private Texture2D histTex = null;
        private Texture2D tempTex = null;
        private Material tfGUIMat = default;
        private TransferFunction tf = default;
        Vector3 tempVector = new Vector3(0, -0.5f, 0);

        void OnEnable()
        {
            InitAndDrawIntensityHistogram();
        }
        private void OnDisable()
        {
            //Destroy(histTex);
        }
        void InitAndDrawIntensityHistogram()
        {
            if (volRendObject == null) return;
            if ((tfGUIMat = Resources.Load<Material>("TFHistogramMat")) == null)
            {
                throw new UnassignedReferenceException("Material has not been found.");
            }

            volRendObject.SetTransferFunctionMode(TFRenderMode.TF1D);
            tf = volRendObject.transferFunction;

            if (histTex == null)
            {
                if (SystemInfo.supportsComputeShaders)
                    histTex = HistogramTextureGenerator.GenerateHistogramTextureOnGPU(volRendObject.dataset);
                else
                    histTex = HistogramTextureGenerator.GenerateHistogramTexture(volRendObject.dataset);
            }
            if (histTex == null)
            {
                throw new UnassignedReferenceException("Hist Texture has not been created successfully.");
            }

            tfGUIMat.SetTexture("_HistTex", histTex);
        }
        private void Update()
        {
            if (RefreshTextures==RefreshTextures_last) return;
            RefreshTextures_last = RefreshTextures;
            Debug.Log("Start Update");
            if (histTex == null) InitAndDrawIntensityHistogram();

            //Update material variables


            //Update control points on the histogram shader
            if (tempTex == null)
            {
                tempTex = new Texture2D(histTex.width, histTex.height, histTex.format, false);
            }
            
            TransferFunctionUtils.DrawAlphaControlPoints(tf.alphaControlPoints, tempTex);
            tfGUIMat.SetTexture("_CPTex", tempTex);
            tfGUIMat.SetTexture("_TFTex", tf.GetTexture());
            Debug.Log("End Update");
        }

        public void toggleActivation()
        {
            if (this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(false);
            }
            else
            {
                this.gameObject.SetActive(true);
            }
        }
    }
}