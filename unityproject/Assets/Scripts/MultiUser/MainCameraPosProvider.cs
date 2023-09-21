using System.Net.Sockets;
using UnityEngine;
using HoloAutopsy.Utils;
using System.Linq;

namespace HoloAutopsy.MultiUser
{
    [RequireComponent(typeof(StreamingVideoPlayer))]
    public class MainCameraPosProvider : MonoBehaviour
    {
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int serverPort = 8889;
        [SerializeField] private Camera mainCamera = default;
        [SerializeField] private Transform coordinatorPlane = default;

        private UdpClient udpClient;
        private StreamingVideoPlayer videoPlayer;

        void Start()
        {
            videoPlayer= GetComponent<StreamingVideoPlayer>();
        }

        // Update is called once per frame
        void Update()
        {
            if (videoPlayer.IsReceivingVideo())
            {
                if (udpClient == null) udpClient = new UdpClient();

                //pose data
                var camLPos = TransformUtils.TransformWorldPositionToLocalTargetSpace(mainCamera.transform.position, coordinatorPlane);
                var camLRot = TransformUtils.TransformWorldRotationToLocalTargetSpace(mainCamera.transform.rotation, coordinatorPlane);
                byte[] data = SerializationUtils.PoseToByteArray(camLPos, camLRot);
                
                try
                {
                    udpClient.Send(data, data.Length, serverIp, serverPort);
                }catch(SocketException e)
                {
                    print(e.ToString());
                }
            }
        }

        void OnDestroy()
        {
            if (udpClient != null) udpClient.Close();
        }
    }
}