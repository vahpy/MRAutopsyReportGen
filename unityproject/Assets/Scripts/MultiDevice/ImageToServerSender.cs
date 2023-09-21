
using System.IO;
using UnityEngine;
using HoloAutopsy.Utils;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.Threading;
using System;
using HoloAutopsy.Record.Photo;
using HoloAutopsy.Record.Logging;

//For Debugging
//[ExecuteInEditMode]
public class ImageToServerSender : MonoBehaviour
{
    [SerializeField] EchoServerBehavior server;
    [SerializeField] private string fileName = "C:\\Users\\vahi0001\\Development\\testcopilot\\vp.jpg";
    [SerializeField] private bool imageSent;
    [SerializeField] private bool ctSent;
    [SerializeField] private string transcribeText;
    [SerializeField] private UnityEvent<BytePacket> ctDisplayListeners;
    [SerializeField] private CTDisplayImageExporter exporter = default;
    // Start is called before the first frame update
    //private UnityEvent<BytePacket> myEvent;
    private int prevTextLength;

    void Start()
    {
        imageSent = true;
        ctSent = true;
        transcribeText = string.Empty;
        prevTextLength = transcribeText.Length;
        if (server == null)
        {
            server = this.GetComponent<EchoServerBehavior>();
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (!ctSent)
        {
            ctSent = true;
            if (ctDisplayListeners != null)
            {
                BytePacket packet = new BytePacket();
                packet.Data = new byte[1] { 0 };
                ctDisplayListeners.Invoke(packet);

                //Debug.Log("Packet:"+packet+", data: "+packet?.Data+", Packet Size: " + packet.Data?.Length + ", going to save at " + Application.persistentDataPath);
                if (packet.Data != null && packet.Data.Length > 20)
                {
                    //File.WriteAllBytes(Application.persistentDataPath + "/rendertexture1.png", packet.Data);
                    //Debug.Log(packet.Data.Length);
                    server?.SendNewMessage("transform&frame", StringUtils.TransformToString(this.transform) + "," + LoggingManager.Instance.frameNum);
                    server?.SendImage(packet.Data);
                }
                else Debug.Log("Couldn't load ");
            }
            //Debug.Log("Event Fired");
        }
        if (!imageSent)
        {
            imageSent = true;
            byte[] byteArrayOfImg = File.ReadAllBytes(fileName);

            if (byteArrayOfImg != null)
            {
                Debug.Log(byteArrayOfImg.Length);
                server?.SendNewMessage("transform&frame", StringUtils.TransformToString(this.transform)+","+LoggingManager.Instance.frameNum);
                server?.SendImage(byteArrayOfImg);
            }
            else Debug.Log("Couldn't load ");
        }
        if (prevTextLength < transcribeText.Length)
        {
            server.SendNewMessage("newwords", transcribeText.Substring(prevTextLength));
            prevTextLength = transcribeText.Length;
        }
    }
    //helper functions

    //public void CallbackFunction(BytePacket dataPacket)
    //{
    //    Debug.Log("Callback function start");
    //    Thread.Sleep(5000);
    //    dataPacket.Data = new byte[] { 2,3,4,5,56,7,6,34,4,34};
    //    Debug.Log("Callback function end");
    //}
}
