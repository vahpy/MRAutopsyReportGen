using HoloAutopsy.MultiDevice;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class EchoServer
{
    private WebSocketServer _server;
    private string ipAddress;
    private int port = 8080;

    public EchoServer(string ipAddress, int port)
    {
        this.ipAddress = ipAddress;
        this.port = port;
    }

    public string GetServerState()
    {
        if (_server == null) return "Server is Null";
        return "Is Listening: " + _server.IsListening;
    }

    public void Start()
    {
        try
        {
            Debug.Log("Echo Server is Starting");
            System.Net.IPAddress ip;
            if (System.Net.IPAddress.TryParse(ipAddress, out ip))
            {
                _server = new WebSocketServer(ip, port);
            }
            else
            {
                _server = new WebSocketServer(port);
            }
            _server.AddWebSocketService<EchoService>("/Echo");

            _server.Start();

            Debug.Log($"Server started on port {port}");
        }
        catch (WebSocketException e)
        {
            Debug.Log(e.ToString());
        }
        //Thread.Sleep(Timeout.Infinite);
    }

    public void Stop()
    {
        if (_server != null)
        {
            _server.Stop();
            Debug.Log($"Server stopped on port {port}");
        }

    }

    public void SendMessageToAllClients(string msg)
    {
        EchoService.SendMessage(msg);
    }

    public void SendImageToAllClients(byte[] imageData)
    {
        EchoService.SendImage(imageData);
    }

    internal class EchoService : WebSocketBehavior
    {
        public static readonly List<EchoService> _clients = new List<EchoService>();
        private int clientID;

        protected override void OnOpen()
        {
            clientID = _clients.Count;
            _clients.Add(this);
            Debug.Log($"New Client ({clientID}) from {this.Context.UserEndPoint.Address}:{this.Context.UserEndPoint.Port} connected"); // in latest version: this.UserEndPoint.Address , Port
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            ClientsMessageInterpreter.PutNewDataInQueue(ID, e.Data, e.RawData, e.IsText, e.IsBinary);
            //foreach (var client in _clients)
            //{
            //    client.Send($"Client {clientID}: {e.Data}");
            //}
        }

        protected override void OnClose(CloseEventArgs e)
        {
            // Remove the client from the set of connected clients
            Debug.Log($"Client ({clientID}) from {this.Context.UserEndPoint.Address}:{this.Context.UserEndPoint.Port} disconnected");
            _clients.Remove(this);
        }

        public static void SendMessage(string msg)
        {
            //Debug.Log(msg);
            foreach (var client in _clients)
            {
                client.Send(msg);
            }
        }
        public static void SendImage(byte[] image)
        {
            foreach (var client in _clients)
            {
                client.Send(image);
            }
        }

    }
}
