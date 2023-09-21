using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Events;
using System.Data;
using HoloAutopsy.Utils;
using HoloAutopsy.Record.Logging;

namespace HoloAutopsy.Record.Photo
{
    //[ExecuteInEditMode]
    public class ShowSavedImg : MonoBehaviour
    {
        [SerializeField] private GameObject viewerParentObj = default;
        [SerializeField] private Material viewerDefaultMat = default; // Don't use it, only copy this
        [SerializeField] private GameObject viewerPlanePrefab = default;
        [SerializeField] private Transform targetVolumesParent = default;
        [SerializeField] private UnityEvent<byte[]> newPhotoListeners = default;
        [SerializeField] private UnityEvent<string,string> photoInfoConsumer = default;
        [SerializeField] private bool debugBtn = true;

        private string lastImageFileName;
        private float lastCheckedTime;

        private HttpImageDownload downloadComp;

        private bool lastTakenPhotoShowed;



        private void Start()
        {
            lastImageFileName = null;
            lastCheckedTime = Time.time;
            lastTakenPhotoShowed = true;
            downloadComp = GetComponent<HttpImageDownload>();
            debugBtn = true;
        }

        private void Update()
        {
            //Debug
            if (!debugBtn)
            {
                debugBtn = true;
                debuggingFunc();
            }

            //control image icon reactions

            //
            if (Time.time > lastCheckedTime + 1 && !downloadComp.isWaitingForResponse)
            {
                downloadComp.FetchLastImagePath();
                lastCheckedTime = Time.time;
            }

            if (lastImageFileName == null || lastImageFileName.Length == 0) //initial
            {
                lastImageFileName = downloadComp.lastImageName;
                lastCheckedTime = Time.time;
                if (lastImageFileName != null) Debug.Log("Previous last image: " + lastImageFileName);
                return;
            }

            //For new images (lastImageFileName is not null here)
            if (!lastImageFileName.Equals(downloadComp.lastImageName) && !downloadComp.isDownloading)
            {
                Debug.Log("Downloading Image" + " | #Debug# Last: " + lastImageFileName + ", New: " + downloadComp.lastImageName);
                lastImageFileName = downloadComp.lastImageName;
                lastTakenPhotoShowed = false;
                downloadComp.RunDownloadLastImage();
            }
            if (!downloadComp.isDownloading && downloadComp.isLastImageFetched && !lastTakenPhotoShowed)
            {
                // file has been downloaded (need more tests)
                Debug.Log("Show image: " + downloadComp.lastDownloadedImageFilePath);
                var viewerPlane = CreateViewerPlane();
                ShowImage(viewerPlane, downloadComp.lastDownloadedImageFilePath);
                List<KeyValuePair<string, float>> sortedHitTargets = HitVolumes(viewerPlane.transform, targetVolumesParent.GetComponentsInChildren<Collider>());


                lastTakenPhotoShowed = true;
            }
        }


        private GameObject CreateViewerPlane()
        {
            GameObject viewPlane = GameObject.Instantiate(viewerPlanePrefab);
            if (viewerParentObj != null)
            {
                viewPlane.transform.parent = viewerParentObj.transform;
            }

            viewPlane.transform.localPosition = Camera.main.transform.position + Camera.main.transform.forward;
            viewPlane.transform.localScale = Vector3.one / 5;
            AlwaysLookAtCamera.AdaptRotation(viewPlane.transform);
            return viewPlane;
        }
        // returns viewer gameobject if successful, otherwise returns null
        public GameObject ShowReceivedImage(byte[] data)
        {
            GameObject viewer = CreateViewerPlane();
            if (viewer == null || viewer.GetComponent<MeshRenderer>() == null) return null;
            Material mat = new Material(viewerDefaultMat.shader);

            if (mat != null && data != null && data.Length > 0)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
                tex.LoadImage(data);
                mat.mainTexture = tex;
                viewer.GetComponent<MeshRenderer>().material = mat;

                Vector3 newScale = viewer.transform.localScale;
                newScale.x *= tex.width / (float)tex.height;
                viewer.transform.localScale = newScale;
                return viewer;
            }
            return null;
        }
        private bool ShowImage(GameObject viewer, string filePath)
        {
            if (viewer == null || viewer.GetComponent<MeshRenderer>() == null) return false;
            Material mat = new Material(viewerDefaultMat.shader);
            byte[] data = File.ReadAllBytes(filePath);
            try
            {
                //photoInfoConsumer?.Invoke("transform", StringUtils.TransformToString(viewer.transform));
                if (LoggingManager.Instance.IsRecording)
                {
                    photoInfoConsumer?.Invoke("transform&frame", StringUtils.TransformToString(viewer.transform) + "," + LoggingManager.Instance.frameNum +","+LoggingManager.Instance.metaFilePath);
                }
                else
                {
                    photoInfoConsumer?.Invoke("transform&frame", StringUtils.TransformToString(viewer.transform) + ",-1,");
                }
                newPhotoListeners?.Invoke(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            //Task.Run(() => {
            //    Debug.Log("Sending data of size \"" + data.Length + "\" bytes to the client asynchronously.");
            //    if (newPhotoListeners == null) Debug.Log("new photo listeners is null");
            //    try
            //    {
            //        newPhotoListeners.Invoke(data);
            //    }catch (Exception ex)
            //    {
            //        Debug.LogException(ex);
            //    }
            //    Debug.Log("Image sent");
            //});
            if (mat != null && data != null && data.Length > 0)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
                tex.LoadImage(data);
                mat.mainTexture = tex;
                viewer.GetComponent<MeshRenderer>().material = mat;
                Vector3 newScale = viewer.transform.localScale;
                newScale.x *= tex.width / (float)tex.height;
                viewer.transform.localScale = newScale;
                return true;
            }
            return false;
        }

        private List<KeyValuePair<string, float>> HitVolumes(Transform origin, Collider[] targets)
        {
            if (origin == null || targets == null) return null;

            Ray ray = new Ray(origin.position, origin.forward);
            Debug.DrawRay(origin.position, origin.forward * 5f);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            List<KeyValuePair<string, float>> hitTargets = new List<KeyValuePair<string, float>>();

            foreach (RaycastHit hit in hits)
            {
                foreach (Collider target in targets)
                {
                    if (hit.collider == target)
                    {
                        hitTargets.Add(new KeyValuePair<string, float>(target.gameObject.name, hit.distance));
                    }
                }
            }

            hitTargets.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            if (hitTargets.Count > 0) return hitTargets;

            return null;
        }


        //Helper functions
        private void debuggingFunc()
        {
            var viewerPlane = CreateViewerPlane();
            ShowImage(viewerPlane, "C:\\Users\\vahi0001\\Development\\testcopilot\\vp.jpg");
            var res = HitVolumes(viewerPlane.transform, targetVolumesParent.GetComponentsInChildren<Collider>());
            if (res != null)
            {
                foreach (var item in res)
                {
                    print(item.Key + ":" + item.Value);
                }
            }
        }
    }
}