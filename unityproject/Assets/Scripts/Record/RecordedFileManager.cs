using HoloAutopsy.Record.Logging;
using Microsoft.MixedReality.OpenXR;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace HoloAutopsy.Record
{
    public class RecordedFileManager : MonoBehaviour
    {
        private const string STR_NOAUDIO = "NOAUDIO";
        private const string STR_NOLOGGING = "NOLOGGING";
        private const string STR_NOTRANSCRIBE = "NOTRANSCRIBE";

        public static RecordedFileManager Instance { get; private set; }

        [SerializeField]
        private RecordingBubbleManager recordingBubbleManager = default;

        [SerializeField]
        private string directoryPath = default;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (string.IsNullOrWhiteSpace(directoryPath)) directoryPath = Application.persistentDataPath;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region PUBLIC_API
        public void LoadAllFiles()
        {
            ListFilesToScene(directoryPath);
        }

        /// <summary>
        /// This function will get the meta info from the file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static RecordFileMetaInfo GetMetaInfo(string filePath, string dirPath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !filePath.EndsWith(".meta")) return null;
            if (dirPath == null) dirPath = Path.GetDirectoryName(filePath);
            if (dirPath.EndsWith("\\") || dirPath.EndsWith("/")) dirPath = dirPath.Substring(0, filePath.Length - 1);


            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 9)
            {
                EWManager.Error($"File `{filePath}` not well-formatted!");
                return null;
            }

            string audio = lines[6], log=lines[7], transcibe = lines[8];
            if (audio.Trim().ToUpper().Equals(STR_NOAUDIO))
            {
                audio = string.Empty;
            }
            else
            {
                audio = dirPath + "\\" + audio;
            }

            if (log.Trim().ToUpper().Equals(STR_NOLOGGING))
            {
                log = string.Empty;
            }
            else
            {
                log = dirPath + "\\" + log;
            }
            if (transcibe.Trim().ToUpper().Equals(STR_NOTRANSCRIBE))
            {
                transcibe = string.Empty;
            }
            else
            {
                transcibe = dirPath + "\\" + transcibe;
            }

            var posStrs = lines[5].Split(',');
            Vector3 pos = new Vector3(float.Parse(posStrs[0]), float.Parse(posStrs[1]), float.Parse(posStrs[2]));
            
            return new RecordFileMetaInfo(audio, log, transcibe,
                lines[0], lines[1], float.Parse(lines[2]), float.Parse(lines[3]), int.Parse(lines[4]), pos);
        }
        
        /// <summary>
        /// This function will add a new meta file to the directory
        /// </summary>
        /// <param name="metaInfo"></param>
        /// <returns></returns>
        public bool AddNewMetaFile(RecordFileMetaInfo metaInfo)
        {
            try
            {
                string[] lines = new string[9];
                lines[0] = metaInfo.realDate;
                lines[1] = metaInfo.realTime;
                lines[2] = metaInfo.frameTime.ToString();
                lines[3] = metaInfo.lengthOfClip.ToString();
                lines[4] = metaInfo.framesCount.ToString();
                lines[5] = metaInfo.position.x + "," + metaInfo.position.y + "," + metaInfo.position.z;
                lines[6] = STR_NOAUDIO;
                lines[7] = STR_NOLOGGING;
                lines[8] = STR_NOTRANSCRIBE;

                //Get file name from path
                if (!string.IsNullOrWhiteSpace(metaInfo.audioFile))
                {
                    var strs = metaInfo.audioFile.Split(new char[] { '/', '\\' });
                    lines[6] = strs[strs.Length - 1];
                }

                if (!string.IsNullOrWhiteSpace(metaInfo.sceneFile))
                {
                    var strs = metaInfo.sceneFile.Split(new char[] { '/', '\\' });
                    lines[7] = strs[strs.Length - 1];
                }
                if (!string.IsNullOrWhiteSpace(metaInfo.transcribeFile))
                {
                    var strs = metaInfo.transcribeFile.Split(new char[] { '/', '\\' });
                    lines[8] = strs[strs.Length - 1];
                }

                string filePath = GenerateMetaFilePath(metaInfo.realDate, metaInfo.realTime);
                File.WriteAllLines(filePath, lines);
                EWManager.Confirm("Meta file saved for this recording at `" + filePath + "`");
            }
            catch (Exception ex)
            {
                EWManager.Error("Couldn't save meta file correctly!\nDetails:" + ex.Message);
                return false;
            }
            return true;
        }

        public static string GenerateMetaFilePath(string date, string time)
        {
            string fileName = date.Replace("/", "-") + "--" + time.Replace(":", ",") + ".meta";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            return filePath;
        }
        //public bool AddNewFiles(string realDate, string realTime, float frameTime, float lengthOfClip, Vector3 position, string audioFilePath, string sceneLogFilePath, string transcibeFilePath)
        //{
        //    try
        //    {
        //        string[] lines = new string[8];
        //        lines[0] = realDate;
        //        lines[1] = realTime;
        //        lines[2] = frameTime.ToString();
        //        lines[3] = lengthOfClip.ToString();
        //        lines[4] = position.x + "," + position.y + "," + position.z;
        //        lines[5] = STR_NOAUDIO;
        //        lines[6] = STR_NOLOGGING;
        //        lines[7] = STR_NOTRANSCRIBE;

        //        //Get file name from path
        //        if (!string.IsNullOrWhiteSpace(audioFilePath))
        //        {
        //            var strs = audioFilePath.Split(new char[] { '/', '\\' });
        //            lines[5] = strs[strs.Length - 1];
        //        }

        //        if (!string.IsNullOrWhiteSpace(sceneLogFilePath))
        //        {
        //            var strs = sceneLogFilePath.Split(new char[] { '/', '\\' });
        //            lines[6] = strs[strs.Length - 1];
        //        }
        //        if (!string.IsNullOrWhiteSpace(transcibeFilePath))
        //        {
        //            var strs = transcibeFilePath.Split(new char[] { '/', '\\' });
        //            lines[7] = strs[strs.Length - 1];
        //        }

        //        string fileName = realDate.Replace("/", "-") + "--" + realTime.Replace(":", ",") + ".meta";
        //        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        //        File.WriteAllLines(filePath, lines);
        //        EWManager.Confirm("Meta file saved for this recording at `" + filePath + "`");
        //    }
        //    catch (Exception ex)
        //    {
        //        EWManager.Error("Couldn't save meta file correctly!\nDetails:" + ex.Message);
        //        return false;
        //    }
        //    return true;
        //}
        #endregion

        #region FUNCTIONALITY
        private void ListFilesToScene(string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath)) return;

            if (dirPath.EndsWith("\\") || dirPath.EndsWith("/")) dirPath = dirPath.Substring(0, dirPath.Length - 1);

            var files = Directory.GetFiles(dirPath, "*.meta", SearchOption.AllDirectories);

            foreach (string filePath in files)
            {
                RecordFileMetaInfo metaInfo = GetMetaInfo(filePath, dirPath);
                if (metaInfo != null)
                {
                    recordingBubbleManager.AddBubble(metaInfo);
                }
            }
        }

        #endregion
    }
    public class RecordFileMetaInfo
    {
        public string audioFile { private set; get; }
        public string sceneFile { private set; get; }
        public string transcribeFile { private set; get; }
        public string realDate { private set; get; }
        public string realTime { private set; get; }
        public float frameTime { private set; get; }
        public float lengthOfClip { private set; get; }
        public int framesCount { private set; get; }
        public Vector3 position { private set; get; }

        public RecordFileMetaInfo(string audioFile, string sceneFile, string transcribeFile, string realDate, string realTime, float frameTime, float lengthOfClip, int framesCount, Vector3 position)
        {
            this.audioFile = audioFile;
            this.sceneFile = sceneFile;
            this.transcribeFile = transcribeFile;
            this.realDate = realDate;
            this.realTime = realTime;
            this.frameTime = frameTime;
            this.lengthOfClip = lengthOfClip;
            this.framesCount = framesCount;
            this.position = position;
        }
    }
}