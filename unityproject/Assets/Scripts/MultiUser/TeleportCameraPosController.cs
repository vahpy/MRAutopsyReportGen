using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using HoloAutopsy.Utils;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

namespace HoloAutopsy.MultiUser
{
    [RequireComponent(typeof(Camera), typeof(StreamVideo))]
    public class TeleportCameraPosController : MonoBehaviour
    {
        [SerializeField] private int serverPort = 8889;
        [SerializeField] private UdpClient udpClient;
        [SerializeField] private IPEndPoint remoteIpEndpoint;
        [SerializeField] private Transform coordinatorPlane = default;
        [SerializeField] private bool enableSmoothing = default;
        [SerializeField, Range(0.1f, 10f)] private float smoothing = 0.1f;
        // Start is called before the first frame update
        private StreamVideo streamer;
        private Camera cameraToStream;

        private Vector3 velocity = Vector3.zero;

        void Start()
        {
            cameraToStream = GetComponent<Camera>();
            streamer = GetComponent<StreamVideo>();
        }

        // Update is called once per frame
        void Update()
        {
            if (streamer.IsStreaming())
            {
                if (udpClient == null)
                {
                    udpClient = new UdpClient(serverPort);
                }
                if (remoteIpEndpoint == null) remoteIpEndpoint = new IPEndPoint(IPAddress.Any, serverPort);
                if (udpClient.Available > 0)
                {
                    //process messages
                    byte[] poseBytes = udpClient.Receive(ref remoteIpEndpoint);
                    //print("Rec pose: " +poseBytes.Length);
                    var pose = SerializationUtils.ByteArrayToPose(poseBytes);


                    Vector3 targetPosition = TransformUtils.TransformLocalPositionToWorldSpace(pose.position, coordinatorPlane);
                    Quaternion targetRotation = TransformUtils.TransformLocalRotationToWorldSpace(pose.rotation, coordinatorPlane);

                    // with smoothing
                    if (enableSmoothing)
                    {
                        cameraToStream.transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothing); ;
                        cameraToStream.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothing);
                    }
                    else
                    {
                        //without smoothing
                        cameraToStream.transform.position = targetPosition;
                        cameraToStream.transform.rotation = targetRotation;
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (udpClient != null) udpClient.Close();
        }
    }
}