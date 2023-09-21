using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityVolumeRendering;

namespace HoloAuopsy
{
    [ExecuteInEditMode]
    public class TransferFunctionView : MonoBehaviour
    {
        [SerializeField] private bool adaptiveManipulator = false;
        [SerializeField] private VolumeRenderedObject volRendObject = default;
        [SerializeField] private Transform slider1 = default;
        [SerializeField] private Transform slider2 = default;


        public List<TFAlphaControlPoint> DefaultAlphaControlPoints = null;
        private Texture2D histTex = null;
        private Material tfGUIMat = default;
        private TransferFunction tf = default;
        Vector3 tempVector = new Vector3(0, -0.5f, 0);

        //private Quaternion slider1DefaultRotation;
        //private Quaternion slider2DefaultRotation;

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
            if (DefaultAlphaControlPoints == null || DefaultAlphaControlPoints.Count < 2)
            {
                DefaultAlphaControlPoints = tf.alphaControlPoints;
                DefaultAlphaControlPoints.Sort((a, b) => (a.dataValue.CompareTo(b.dataValue)));
            }

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
            tfGUIMat.SetFloat("_Slider1", 0f);
            tfGUIMat.SetFloat("_Slider2", 1f);
        }
        private void Update()
        {
            if (histTex == null) InitAndDrawIntensityHistogram();
            //Stick sliders to the buttom of the plane
            tempVector.x = Mathf.Clamp(slider1.localPosition.x, -0.5f, 0.5f);
            slider1.localPosition = tempVector;
            tempVector.x = Mathf.Clamp(slider2.localPosition.x, -0.5f, 0.5f);
            slider2.localPosition = tempVector;
            slider1.localRotation = Quaternion.identity;
            slider2.localRotation = Quaternion.identity;

            //Update material variables
            if (slider1.hasChanged || slider2.hasChanged)
            {
                tfGUIMat.SetFloat("_Slider1", slider1.localPosition.x + 0.5f);
                tfGUIMat.SetFloat("_Slider2", slider2.localPosition.x + 0.5f);
                slider1.hasChanged = false;
                slider2.hasChanged = false;
                float start = Mathf.Min(slider1.localPosition.x, slider2.localPosition.x);
                float end = Mathf.Max(slider1.localPosition.x, slider2.localPosition.x);
                if (adaptiveManipulator) ChangeTransferFunctionMode2(start + 0.5f, end + 0.5f);
                else ChangeTransferFunctionMode1();
            }

            //Update control points on the histogram shader
            //List<TFAlphaControlPoint> alphaControlPoints = null;
            //tfGUIMat.SetVectorArray("_ControlPoints", alphaContrlPoints);
            //tfGUIMat.SetVectorArray()
        }

        void ChangeTransferFunctionMode1()
        {
            if (tf.alphaControlPoints.Count != 0) tf.alphaControlPoints.Clear();
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(0, 0.0f));
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(1, 0.0f));
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(slider1.localPosition.x + 0.49f, 0.0f));
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(slider1.localPosition.x + 0.5f, 0.2f));
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(slider2.localPosition.x + 0.51f, 0.0f));
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(slider2.localPosition.x + 0.5f, 0.2f));
            tf.GenerateTexture();
        }

        void ChangeTransferFunctionMode2(float start, float end)
        {
            List<TFAlphaControlPoint> alphas = tf.alphaControlPoints;
            alphas.Sort((a, b) => (a.dataValue.CompareTo(b.dataValue)));
            TFAlphaControlPoint cp;
            for (int i = 0; i < alphas.Count; i++)
            {
                cp = alphas[i];
                if (cp.dataValue < start || cp.dataValue > end)
                {
                    alphas[i] = new TFAlphaControlPoint(cp.dataValue, 0.0f);
                }
                else
                {
                    alphas[i] = DefaultAlphaControlPoints[i];
                }
            }
            tf.GenerateTexture();
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

        public void toggleTFManipulatorMode()
        {
            if (adaptiveManipulator)
            {
                adaptiveManipulator = false;
            }
            else
            {
                adaptiveManipulator = true;
            }
        }
    }
}