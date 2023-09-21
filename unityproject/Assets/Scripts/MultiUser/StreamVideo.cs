using System;
using System.Net.Sockets;
using UnityEngine;

namespace HoloAutopsy.MultiUser
{
    [RequireComponent(typeof(Camera))]
    public class StreamVideo : MonoBehaviour
    {
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int serverPort = 8888;
        private RenderTexture targetRenderTexture;
        private UdpClient udpClient;
        private Camera cameraToStream;
        [SerializeField] private bool streamVideo = default;

        private byte frameID = 0;

        private const int MAX_UDP_DATA_SIZE = 64500;
        private const int CAMERA_WIDTH = 2048;
        private const int CAMERA_HEIGHT = 1080;

        void Start()
        {
            cameraToStream = GetComponent<Camera>();
            cameraToStream.fieldOfView = Camera.main.fieldOfView;
            cameraToStream.aspect = Camera.main.aspect;
            targetRenderTexture = new RenderTexture((int)(1080 * Camera.main.aspect), 1080, 0, RenderTextureFormat.ARGB32);
            targetRenderTexture.filterMode = FilterMode.Point;
            targetRenderTexture.wrapMode = TextureWrapMode.Clamp;
            cameraToStream.targetTexture = targetRenderTexture;
            print("Field of view: " + Camera.main.fieldOfView + ", aspect ratio: " + Camera.main.aspect);
            print("Width: " + targetRenderTexture.width + ", Height: " + targetRenderTexture.height);
        }

        void Update()
        {
            if (streamVideo)
            {
                if(cameraToStream.aspect != Camera.main.aspect)
                {
                    print("Aspect Ratio, Camera to Stream:" + cameraToStream.aspect + ", Main: " + Camera.main.aspect);
                    cameraToStream.aspect = Camera.main.aspect;
                    Destroy(targetRenderTexture);
                    targetRenderTexture = null;
                    targetRenderTexture = new RenderTexture((int)(1080 * Camera.main.aspect), 1080, 0, RenderTextureFormat.ARGB32);
                    targetRenderTexture.filterMode = FilterMode.Point;
                    targetRenderTexture.wrapMode = TextureWrapMode.Clamp;
                    cameraToStream.targetTexture = targetRenderTexture;
                }
                if (cameraToStream.fieldOfView != Camera.main.fieldOfView)
                {
                    print("Field of View, Camera to Stream:" + cameraToStream.fieldOfView + ", Main: " + Camera.main.fieldOfView);
                    cameraToStream.fieldOfView = Camera.main.fieldOfView;
                }
                if (udpClient == null)
                {
                    udpClient = new UdpClient();
                    //print("Receive Buffer Size: " + udpClient.Client.ReceiveBufferSize);
                    udpClient.Client.ReceiveBufferSize = udpClient.Client.ReceiveBufferSize * 10;
                }
                Texture2D tex = new Texture2D(targetRenderTexture.width, targetRenderTexture.height, TextureFormat.RGB24, false);
                cameraToStream.Render();
                RenderTexture.active = targetRenderTexture;
                tex.ReadPixels(new Rect(0, 0, targetRenderTexture.width, targetRenderTexture.height), 0, 0);
                RenderTexture.active = null;
                byte[] imageBytes = tex.EncodeToJPG(60);
                //try
                //{
                // 2 bytes reserved : 1 - Message Id, 2 - Total Packets, 3 - Packet Number
                int size = imageBytes.Length / MAX_UDP_DATA_SIZE + (imageBytes.Length % MAX_UDP_DATA_SIZE == 0 ? 0 : 1);
                for (int i = 0; i < size; i++)
                {
                    var firstIdx = i * MAX_UDP_DATA_SIZE;
                    var dataBytes = CopyArray(imageBytes, firstIdx, 3, MAX_UDP_DATA_SIZE);
                    dataBytes[0] = frameID;
                    dataBytes[1] = (byte)size;
                    dataBytes[2] = (byte)i;
                    udpClient.Send(dataBytes, dataBytes.Length, serverIp, serverPort);
                }
                //print("frame sent #" + frameID + " in " + size + " packets");
                //udpClient.Send(imageBytes, imageBytes.Length, serverIp, serverPort);
                //}
                //catch (SocketException e)
                //{

                //}
                Destroy(tex);
                frameID++;
            }
        }

        public static byte[] CopyArray(byte[] sourceArray, int startIdx, int initialOffset, int size)
        {
            if (size + startIdx > sourceArray.Length)
            {
                size = sourceArray.Length - startIdx;
            }
            byte[] bytes = new byte[size + initialOffset];
            for (int i = 0; i < size; i++)
            {
                bytes[i + initialOffset] = sourceArray[startIdx + i];
            }
            return bytes;
        }

        void OnDestroy()
        {
            if (udpClient != null) udpClient.Close();
        }

        public bool IsStreaming()
        {
            return streamVideo;
        }
    }
}