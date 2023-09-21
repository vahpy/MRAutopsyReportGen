using Microsoft.MixedReality.OpenXR;
using QRTracking;
using System;
using UnityEngine;

public class MatchPlaneQRCode : MonoBehaviour
{
    [SerializeField] private QRCodesManager qrCodeManager = default;
    SpatialGraphNode node;

    private Pose pose;
    private bool newPoseDetected;
    private float sideLength;
    private object lockRef = new object();
    public bool planeAdjusted { private set; get; }

    private void Awake()
    {
        this.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        lock (lockRef)
        {
            if (newPoseDetected)
            {
                print("new pos loc: " + pose.position);
                newPoseDetected = false;
                UpdateTransform();
                planeAdjusted = true;
                this.enabled = false;
            }
        }
    }
    void OnEnable()
    {
        if (qrCodeManager == null) qrCodeManager = GetComponent<QRCodesManager>();
        qrCodeManager.QRCodeAdded += NewQRCode;
        qrCodeManager.QRCodeUpdated += NewQRCode;
        newPoseDetected = false;
        planeAdjusted = false;
        print("qr manager starts tracking");
    }

    public void UpdateTransform()
    {
        if (this.transform.parent != null)
        {
            Vector3 ps = this.transform.parent.localScale;
            Vector3 cs = this.transform.localScale;
            this.transform.parent.SetPositionAndRotation(pose.position, pose.rotation);
            this.transform.parent.Rotate(new Vector3(1, 0, 0), 90, Space.Self);
            this.transform.parent.Translate(new Vector3(sideLength/2, 0, -sideLength/2), Space.Self);
            float ratioX = sideLength / (cs.x * ps.x * 10);
            float ratioY = sideLength / (cs.y * ps.y *10);
            float ratioZ = sideLength / (cs.z * ps.z * 10);
            this.transform.parent.localScale = new Vector3(ratioX * ps.x, ratioY * ps.y, ratioZ * ps.z);
        }
    }

    public void NewQRCode(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> args)
    {
        lock (lockRef)
        {
            var code = args.Data;
            try
            {
                if (node == null)
                {
                    node = SpatialGraphNode.FromStaticNodeId(code.SpatialGraphNodeId);
                    //print("Node declared for " + code.SpatialGraphNodeId);
                }
                if (node.TryLocate(FrameTime.OnUpdate, out pose))
                {
                    newPoseDetected = true;
                    sideLength = code.PhysicalSideLength;
                    //print("new pose detected, info:" + pose.position + " | " + pose.rotation + " , size: " + code.PhysicalSideLength);
                    //try
                    //{
                    //    if (CameraCache.Main.transform.parent != null)
                    //    {
                    //        pose = pose.GetTransformedBy(CameraCache.Main.transform.parent);
                    //    }
                    //    print("new pose detected (after), info:" + pose.position + " | " + pose.rotation + " , size: " + code.PhysicalSideLength);
                    //}
                    //catch (Exception e)
                    //{
                    //    print(e.Message);
                    //}
                    //if (this.transform.parent != null)
                    //{

                    //}
                    //else
                    //{
                    //    print("before: " + this.transform.position + " | " + this.transform.rotation + " | " + this.transform.localScale);
                    //    this.transform.SetPositionAndRotation(pose.position, pose.rotation);
                    //    print("after: " + this.transform.position + " | " + this.transform.rotation + " | " + this.transform.localScale);
                    //}

                    //if(this.transform.parent != null)
                    //{
                    //    this.transform.parent.SetPositionAndRotation(pose.position, pose.rotation);
                    //}
                }
            }
            catch (Exception ex)
            {
                print(ex);
            }
        }
    }
}