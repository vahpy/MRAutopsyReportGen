using HoloAutopsy.Utils;
using System.IO;
using UnityEngine;


namespace HoloAutopsy.Record.Photo
{
    [ExecuteInEditMode]
    public class CTDisplayImageExporter : MonoBehaviour
    {
        // Start is called before the first frame update
        private Camera childCamera;

        private float defaultRatio;

        void Start()
        {
            if (childCamera == null)
            {
                childCamera = this.GetComponentInChildren<Camera>();
            }
            if (childCamera == null) return;
            var defaultScale = this.transform.lossyScale;
            var defaultOrthSize = childCamera.orthographicSize;
            //Debug.Log("Default Scale = " + defaultScale.x + ", orth size = " + childCamera.orthographicSize);
            defaultRatio = defaultOrthSize / defaultScale.x;
            //Debug.Log("Aspect Ratio: " + defaultRatio);
        }

        #region PUBLIC_API
        public void GetPNGFromDisplay(BytePacket packet)
        {
            packet.Data = GetPNGFromDisplay();
            //packet.localTransform = this.transform;
        }
        public byte[] GetPNGFromDisplay()
        {
            if (childCamera == null) return null;

            // adjust the camera
            updateCameraSize();
            var renderTexture = childCamera.targetTexture;

            if (renderTexture == null)
            {
                Debug.LogError("Target texture of this display is null.");
            }
            else
            {
                var bytes = GetBytesFromRenderTexture(renderTexture);
                //Debug.Log("Length of byte: " + bytes);
                return bytes;
            }
            return null;
        }
        public void SaveDisplayToPNGFile(string filePath)
        {
            if (childCamera == null) return;

            // adjust the camera
            updateCameraSize();
            var renderTexture = childCamera.targetTexture;

            if (renderTexture == null)
            {
                Debug.LogError("Target texture of this display is null.");
            }
            else
            {
                File.WriteAllBytes(filePath, GetBytesFromRenderTexture(renderTexture));
                //Debug.Log("Saved an image of display at: " + filePath);
            }
        }
        #endregion

        #region HELPER_FUNCTIONS
        private void updateCameraSize()
        {
            childCamera.orthographicSize = defaultRatio * this.transform.lossyScale.x;
        }

        private static byte[] GetBytesFromRenderTexture(RenderTexture renderTexture)
        {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            return bytes;
        }
        #endregion
    }
}