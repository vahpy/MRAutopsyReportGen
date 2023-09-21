using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

public class EchoServerBehavior : MonoBehaviour
{
    [SerializeField] private string ipAddress = "127.0.0.1";
    [SerializeField] private int port = 8080;
    [SerializeField] private string username = default;
    [SerializeField] private bool connect = default;

    private EchoServer server;
    private UnityEvent<string> onSendMessage;
    private UnityEvent<byte[]> onSendImage;

    private bool prevConnect = false;

    private string preState;
    private CancellationToken taskCancellationToken;
    private Task currentConnectionTask;
    private void Start()
    {
        prevConnect = !connect;
    }
    private void OnDisable()
    {
        server?.Stop();
    }
    private void Update()
    {
        //start connect
        if (prevConnect != connect)
        {
            prevConnect = connect;
            if (connect)
            {
                server = new EchoServer(ipAddress, port);

                onSendMessage = new UnityEvent<string>();
                onSendMessage.AddListener(server.SendMessageToAllClients);

                onSendImage = new UnityEvent<byte[]>();
                onSendImage.AddListener(server.SendImageToAllClients);

                Debug.Log("New thread is being created for EchoServer");
                taskCancellationToken = new CancellationToken();
                currentConnectionTask = new Task(server.Start, taskCancellationToken);
                currentConnectionTask.Start();

                preState = "";
            }
            else
            {
                //disconnect
                server?.Stop();
                currentConnectionTask?.Dispose();
                currentConnectionTask = null;
                server = null;
            }
        }

        //end connect
        string newState = server?.GetServerState();
        if (preState != newState)
        {
            preState = newState;
            Debug.Log(server?.GetServerState());
        }
    }

    public void SendImage(byte[] imageData)
    {
        //Debug.Log("Send Image to Server with size: " + imageData.Length);
        //if (onSendImage == null)
        //{
        //    Debug.Log("onSendMessage is null");
        //}
        SendNewMessage("setUser", username);
        onSendImage?.Invoke(imageData);
    }

    public void SendNewMessage(string func, string data)
    {
        SendNewMessage("$" + (username != null ? username : "") + "$%" + (func != null ? func : "") + "%" + (data != null ? data : ""));
    }

    protected void SendNewMessage(string msg)
    {
        //if (onSendMessage == null)
        //{
        //    Debug.Log("onSendMessage is null");
        //}
        //Debug.Log(msg);
        onSendMessage?.Invoke(msg);
    }

    public void SendGestureDetection(string gesture)
    {
        //Debug.Log("Event Received:"+gesture);
        SendNewMessage(gesture, null);
    }

    //This is a special function for Speech To Text service in my app, don't use for anything else
    public void SendSpeechToTextMsg(string newWords)
    {
        print("EchoServerBehavior: " + Thread.CurrentThread.ManagedThreadId);
        if (newWords== null) print("SendSpeechToTextMsg(null)");
        else print("SendSpeechToTextMsg( " + newWords + ")");
        SendNewMessage("newwords", newWords);
    }

    public void SendImageAsync(byte[] data)
    {
        Task.Run(() => { server?.SendImageToAllClients(data); });
    }
}
