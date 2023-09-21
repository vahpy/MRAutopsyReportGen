using HoloAutopsy.Record.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace HoloAutopsy.MultiDevice
{
    //[ExecuteInEditMode]
    [RequireComponent(typeof(CursorProjection))]
    public class ClientsMessageInterpreter : MonoBehaviour
    {
        private CursorProjection cursorManager;
        [SerializeField] private int x = 0;
        public static ClientsMessageInterpreter Instance { private set; get; }
        private List<Tuple<string, string, byte[], bool, bool>> dataQueue;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                cursorManager = this.GetComponent<CursorProjection>();
            }
            dataQueue = new List<Tuple<string, string, byte[], bool, bool>>();
        }

        public static ClientsMessageInterpreter GetInstance()
        {
            return Instance;
        }

        void Update()
        {
            lock (Instance.dataQueue)
            {
                foreach (var pair in dataQueue)
                {
                    InterpretMessage(pair.Item1, pair.Item2, pair.Item3, pair.Item4, pair.Item5);
                }
                dataQueue.Clear();
            }
        }

        public static void PutNewDataInQueue(string clientID, string data, byte[] rawData, bool isText, bool isBinary)
        {
            lock (Instance.dataQueue)
            {
                Instance.dataQueue.Add(new Tuple<string, string, byte[], bool, bool>(clientID, data, rawData, isText, isBinary));
            }
        }

        public static void InterpretMessage(string clientID, string data, byte[] rawData, bool isText, bool isBinary)
        {
            if (isText && data != null && data.Length > 0)
            {
                Instance.cursorManager.NewTextDataFromClient(data);
            }
            else if (isBinary && rawData != null && rawData.Length > 0)
            {
                Debug.Log("Binary Data Received (" + clientID + ") with Length: " + rawData.Length);
                Instance.cursorManager.NewBlobDataFromClient(rawData);
            }
            else
            {
                Debug.LogWarning("Data received from " + clientID + " is not compatible (isText: " + isText + ", isBinary: " + isBinary + " ).");
            }
        }
    }
}