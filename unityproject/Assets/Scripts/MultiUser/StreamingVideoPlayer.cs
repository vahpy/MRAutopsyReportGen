using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace HoloAutopsy.MultiUser
{
    public class StreamingVideoPlayer : MonoBehaviour
    {
        public Material materialToReceive;
        public int serverPort = 8888;
        private UdpClient udpClient;
        private IPEndPoint remoteIpEndpoint;
        [SerializeField] private RawImage imageDisplay;
        [SerializeField] private bool receiveTeleport = default;
        private Texture2D tex;
        private bool last_ReceiveTeleport;

        private byte prv_packet_id = 255;
        private List<Tuple<int, byte[]>> currentPackets;
        private float initialAspectRatio;
        void Awake()
        {
            currentPackets = new List<Tuple<int, byte[]>>();
        }
        void Start()
        {
            last_ReceiveTeleport = false;
            initialAspectRatio = Camera.main.aspect;
            tex = new Texture2D((int)(1080 * initialAspectRatio), 1080, TextureFormat.RGB24, false);
            if (imageDisplay != null)
            {
                imageDisplay.texture = tex;
                imageDisplay.gameObject.SetActive(receiveTeleport);
            }
        }

        void Update()
        {
            if (last_ReceiveTeleport != receiveTeleport)
            {
                last_ReceiveTeleport = receiveTeleport;
                imageDisplay.gameObject.SetActive(receiveTeleport);
            }
            if (imageDisplay == null) return;
            if (receiveTeleport)
            {
                if (initialAspectRatio != Camera.main.aspect)
                {
                    print("Initial Aspect ratio changed in receiver, from " + initialAspectRatio + " to " + Camera.main.aspect);
                    initialAspectRatio= Camera.main.aspect;
                    Destroy(tex);
                    tex = null;
                    tex = new Texture2D((int)(1080 * initialAspectRatio), 1080, TextureFormat.RGB24, false);
                    imageDisplay.texture = tex;
                }
                if (udpClient == null)
                {
                    udpClient = new UdpClient(serverPort);
                    //print("Receive Buffer Size: " + udpClient.Client.ReceiveBufferSize);
                    udpClient.Client.ReceiveBufferSize = udpClient.Client.ReceiveBufferSize * 10;
                }

                if (remoteIpEndpoint == null) remoteIpEndpoint = new IPEndPoint(IPAddress.Any, serverPort);
                while (udpClient.Available > 0)
                {
                    byte[] packetBytes = udpClient.Receive(ref remoteIpEndpoint);
                    if (packetBytes != null)
                    {
                        var frameID = packetBytes[0];
                        var packetsCount = packetBytes[1];
                        if (frameID != prv_packet_id)
                        {
                            //print("new frame: " + frameID);
                            prv_packet_id = frameID;
                            currentPackets.Clear();
                        }

                        currentPackets.Add(new Tuple<int, byte[]>(packetBytes[2], packetBytes.SubArray(3, packetBytes.Length - 3)));

                        if (currentPackets.Count == packetsCount)
                        {
                            currentPackets.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                            if (currentPackets[currentPackets.Count - 1].Item1 == packetsCount - 1)
                            {
                                // Received all data
                                //print("frame received #" + frameID + " in " + currentPackets.Count + " packets");
                                int finalArraySize = 0;
                                foreach (var packet in currentPackets)
                                {
                                    finalArraySize += packet.Item2.Length;
                                }
                                byte[] imageBytes = new byte[finalArraySize];
                                int finalIdx = 0;
                                for (int i = 0; i < currentPackets.Count; i++)
                                {
                                    byte[] currentArray = currentPackets[i].Item2;
                                    int byteArraySize = currentArray.Length;
                                    for (int j = 0; j < byteArraySize; j++)
                                    {
                                        imageBytes[finalIdx] = currentArray[j];
                                        finalIdx++;
                                    }
                                }
                                //show image
                                tex.LoadImage(imageBytes);
                                materialToReceive.mainTexture = tex;
                            }
                        }
                    }
                }
                //if (udpClient.Available > 0)
                //{
                //    byte[] imageBytes = udpClient.Receive(ref remoteIpEndpoint);
                //    tex.LoadImage(imageBytes);
                //    materialToReceive.mainTexture = tex;
                //}

            }
        }

        void OnDestroy()
        {
            Destroy(tex);
            if (udpClient != null) udpClient.Close();
        }

        public bool IsReceivingVideo()
        {
            return receiveTeleport;
        }
    }
    public static class Ext
    {
        //Extenstion
        public static T[] SubArray<T>(this T[] array, int startIndex, int length)
        {
            int num;
            if (array == null || (num = array.Length) == 0)
            {
                return new T[0];
            }

            if (startIndex < 0 || length <= 0 || startIndex + length > num)
            {
                return new T[0];
            }

            if (startIndex == 0 && length == num)
            {
                return array;
            }

            T[] array2 = new T[length];
            Array.Copy(array, startIndex, array2, 0, length);
            return array2;
        }
    }
}