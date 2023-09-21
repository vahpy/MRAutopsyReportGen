using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace HoloAutopsy.Record.Photo
{
    public class HttpImageDownload : MonoBehaviour
    {
        //Get all files in the folder: https://192.168.10.193/api/filesystem/apps/files?knownfolderid=Pictures&packagefullname=%5C&path=%5C%5CCamera%20Roll
        //Get image: https://192.168.10.193/api/filesystem/apps/file?knownfolderid=Pictures&filename=20220130_190431_HoloLens.jpg&packagefullname=%5C&path=%5C%5CCamera%20Roll
        //Download it!

        [SerializeField] private string remoteHostName = default;
        [SerializeField] private string username = default;
        [SerializeField] private string password = default;

        //
        public bool isWaitingForResponse { get; private set; }
        private bool wasReqSucc;
        public string lastImageName { get; private set; }
        public string lastDownloadedImageFilePath { get; private set; }
        public bool isLastImageFetched { get; private set; }
        public bool isDownloading { get; private set; }
        private bool runDownloadLastImage;
        //

        void Start()
        {
            isWaitingForResponse = false;
            isLastImageFetched = false;
            isDownloading = false;
            lastImageName = null;
            runDownloadLastImage = false;
            FetchLastImagePath();
        }

        public void RunDownloadLastImage()
        {
            runDownloadLastImage = true;
            isLastImageFetched = false;
        }
        public void FetchLastImagePath()
        {
            if (isWaitingForResponse) return;

            isWaitingForResponse = true;
            wasReqSucc = false;
            //lastImageName = string.Empty;
            string encodedString = Base64Encode(username + ":" + password);
            string uri = "https://" + remoteHostName + "/api/filesystem/apps/files?knownfolderid=Pictures&packagefullname=%5C&path=%5C%5CCamera%20Roll";
            StartCoroutine(GetRequest(uri, new string[] { "authorization", "Basic " + encodedString }));
        }
        void Update()
        {
            if (runDownloadLastImage && !isLastImageFetched && !isDownloading /*&& lastImageName != null && lastImageName.Length != 0*/)
            {
                runDownloadLastImage = false;
                string storeFilePath = System.IO.Path.Combine(Application.persistentDataPath, lastImageName);
                string encodedString = Base64Encode(username + ":" + password);
                string uri = "https://" + remoteHostName + "/api/filesystem/apps/file?knownfolderid=Pictures&filename=" + lastImageName + "&packagefullname=%5C&path=%5C%5CCamera%20Roll";
                isDownloading = true;
                StartCoroutine(DownloadImage(uri, new string[] { "authorization", "Basic " + encodedString }, storeFilePath));
            }
        }

        private string ExtractLastestFile(string jsonString, string extension = null)
        {
            int lastIndex = jsonString.LastIndexOf(']');

            //format the extension
            extension = extension.ToLower();
            if (extension[0] != '.')
            {
                extension = "." + extension;
            }


            while (lastIndex > 0)
            {
                lastIndex = jsonString.LastIndexOf('}', lastIndex);
                int startIndex = jsonString.LastIndexOf('{', lastIndex);

                //One object selected
                int idIdx = jsonString.IndexOf("Id", startIndex, lastIndex - startIndex + 1);
                int idEndIdx = jsonString.IndexOf('"', idIdx + 7);
                string fileName = jsonString.Substring(idIdx + 7, idEndIdx - idIdx - 7);
                if (extension == null)
                {
                    return fileName;
                }
                else
                {
                    if (fileName.Substring(fileName.Length - extension.Length).ToLower().Equals(extension))
                    {
                        return fileName;
                    }
                }
                lastIndex = startIndex - 1;
            }
            Debug.LogWarning("Couldn't find a file with \"" + extension + "\" extension in: " + jsonString);
            return null;
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private IEnumerator DownloadImage(string uri, string[] headers, string storeFilePath)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                webRequest.certificateHandler = new AcceptAllCertificates();
                if (headers != null && headers.Length > 0)
                {
                    for (int i = 0; i < headers.Length; i += 2)
                    {
                        webRequest.SetRequestHeader(headers[i], headers[i + 1]);
                    }
                }

                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarning(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        File.WriteAllBytes(storeFilePath, webRequest.downloadHandler.data);
                        lastDownloadedImageFilePath = storeFilePath;
                        isLastImageFetched = true;
                        break;
                }
            }
            isDownloading = false;
        }

        private IEnumerator GetRequest(string uri, string[] headers)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                webRequest.certificateHandler = new AcceptAllCertificates();
                if (headers != null && headers.Length > 0)
                {
                    for (int i = 0; i < headers.Length; i += 2)
                    {
                        webRequest.SetRequestHeader(headers[i], headers[i + 1]);
                    }
                }

                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogWarning(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarning(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                        wasReqSucc = true;
                        lastImageName = ExtractLastestFile(webRequest.downloadHandler.text, ".jpg");
                        break;
                }
                isWaitingForResponse = false;
            }
        }
        private class AcceptAllCertificates : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
    }
}