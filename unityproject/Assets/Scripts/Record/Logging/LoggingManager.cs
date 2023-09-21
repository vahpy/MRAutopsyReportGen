///
/// This class is designed to capture and record changes made to a scene, primarily transformations. It's important to note that only frames with changes are recorded,
/// along with the elements that were modified. Therefore, to replay a specific frame, you need to replay all the changes starting from the first frame,
/// which always captures the current state of all captured objects.
/// 
using QRTracking;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WebSocketSharp;

namespace HoloAutopsy.Record.Logging
{
    public class LoggingManager : MonoBehaviour
    {
        public enum LoggingState { IDLE, RECORDING, REPLAYING, PAUSED };
        private static LoggingManager _instance;
        public static LoggingManager Instance
        {
            get { return _instance; }
        }

        [SerializeField]
        private List<GameObject> registeredObjects = default;

        // control avatar
        [SerializeField] private GameObject avatarModel = default;
        [SerializeField] private HeadDirectionTracker HeadTracking;
        [SerializeField] private HandTracking rightHandTracking;
        [SerializeField] private HandTracking leftHandTracking;
        [SerializeField] private bool showAvatar = default;

        //state
        private LoggingState state;
        private LoggingState lastState;


        //Logging
        public string metaFilePath { private set; get; }
        public string filePath { private set; get; }
        private Dictionary<string, List<ObjectLogger>> nameToComponentObj;
        private List<ObjectLogger> loggers;

        //Recording
        public int frameNum { private set; get; }
        public float frameTime { private set; get; }

        //Replay
        private Dictionary<int, string[][]> updatesOnEachFrame = null;
        private List<int> recordedFramesList = null;
        private int recordedFramesListIdx;
        private int pauseFrame = 0;
        private float pauseTime = int.MaxValue;
        public RecordFileMetaInfo MetaInfo { private set; get; }
        //Undo
        private List<string> lastObjectsName = null;

        //public float LengthOfClip { private set; get; }

        #region PUBLIC_API
        // property-like functions
        public bool IsPlaying { get { return state == LoggingState.REPLAYING; } }
        public bool IsRecording { get { return state == LoggingState.RECORDING; } }
        public bool IsIdle { get { return state == LoggingState.IDLE; } }

        public bool IsPaused { get { return state == LoggingState.PAUSED; } }

        // plays the recorded file from leadinTime msecs before target frame, to the target frame

        public void SetRecordedFile(RecordFileMetaInfo metaInfo)
        {
            this.MetaInfo = metaInfo;
        }
        public void PlayFrames(float leadinTime, int targetFrameNum)
        {
            StopPlaying();
            LoadPlay(this.MetaInfo.sceneFile);

            this.frameTime = GetEstimatedRecordedFrameTime(targetFrameNum) - leadinTime;
            this.pauseFrame = targetFrameNum;
            this.pauseTime = GetEstimatedRecordedFrameTime(targetFrameNum);
            //print("Frame time: " + this.frameTime + ", End Frame:" + this.endFrame);
            if (this.frameTime < 0) this.frameTime = 0;
            if (frameTime > this.MetaInfo.lengthOfClip) frameTime = this.MetaInfo.lengthOfClip;
        }
        public void ShiftFrameTime(float shiftTime)
        {
            frameTime += shiftTime;
            if (frameTime < 0) frameTime = 0;
            if (frameTime > this.MetaInfo.lengthOfClip) frameTime = this.MetaInfo.lengthOfClip;
            if (shiftTime < 0) frameNum = 0; // in case it is before the current time, it needs to start from the first frame to cap
        }
        public bool RecordAt(string file, string startDate = "", string startTime = "")
        {
            if (this.IsRecording) return false;
            if (file == null || file.Length <= 0)
            {
                EWManager.Error("File path \"" + file + "\" is not valid!");
                return false;
            }
            this.filePath = file;
            if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(startDate)) this.metaFilePath = RecordedFileManager.GenerateMetaFilePath(startDate, startTime);
            state = LoggingState.RECORDING;
            return true;
        }
        public void LoadPlay(string file)
        {
            if (file == null || !File.Exists(file)) throw new FileNotFoundException("File does not exist at " + file);
            this.filePath = file;
            state = LoggingState.REPLAYING;
            frameNum = 0;
            frameTime = 0;
            recordedFramesListIdx = 0;
            ReadAllDataToDic();
            //EWManager.Log($"Playing scene from file: {this.filePath}");
        }
        public void Resume()
        {
            if (this.IsPaused)
            {
                if (pauseFrame <= frameNum)
                {
                    pauseFrame = int.MaxValue; // next time resume, it continue following frames.)
                    pauseTime = int.MaxValue;
                }
                state = LoggingState.REPLAYING;
            }
        }
        public bool StopRecording()
        {
            this.filePath = string.Empty;
            state = LoggingState.IDLE;
            return true;
        }
        public void Pause()
        {
            if (this.IsPlaying)
            {
                state = LoggingState.PAUSED;
            }
        }
        public void StopPlaying()
        {
            state = LoggingState.IDLE;
            frameTime = 0;
            frameNum = 0;
        }
        public void PlayOneFrame(string logData)
        {
            print("playoneframe start");
            if (nameToComponentObj == null || string.IsNullOrEmpty(logData))
            {
                return;
            }

            string[] data = logData.Split(',');
            if (data.Length == 0)
            {
                return;
            }

            string objectName = data[0];
            lastObjectsName = new List<string> { objectName };

            foreach (ObjectLogger ol in nameToComponentObj[objectName])
            {

                try
                {
                    print("step 3 on " + ol.GetName() + " and component name: " + ol.ToString());
                    ol.Call(data);
                }
                catch (Exception ex)
                {
                    print(ex.Message);
                }
            }
        }
        public void UndoOnlyLastFrame()
        {
            if (lastObjectsName != null)
            {
                foreach (string objName in lastObjectsName)
                {
                    var objLoggers = nameToComponentObj[objName];
                    if (objLoggers != null)
                    {
                        foreach (ObjectLogger objLog in objLoggers)
                        {
                            objLog.Undo();
                        }
                    }
                }
            }
        }

        public void UndoOnlyLastFrame(string objName)
        {
            var objLoggers = nameToComponentObj[objName];
            if (objLoggers != null)
            {
                foreach (ObjectLogger objLog in objLoggers)
                {
                    objLog.Undo();
                }
            }
        }

        public void Idle()
        {
            state = LoggingState.IDLE;
        }

        #endregion

        #region UTIL_FUNCTIONS
        // Assigns a text data (transform), if any error returns false
        public static bool AssignTranfrom(GameObject obj, string data, bool preserveScale = false)
        {
            if (data == null) return false;
            string[] tokens = data.Split(',');

            try
            {
                obj.transform.localPosition = new Vector3(float.Parse(tokens[2]), float.Parse(tokens[3]), float.Parse(tokens[4]));
                obj.transform.localRotation = new Quaternion(float.Parse(tokens[5]), float.Parse(tokens[6]), float.Parse(tokens[7]), float.Parse(tokens[8]));
                if (!preserveScale) obj.transform.localScale = new Vector3(float.Parse(tokens[9]), float.Parse(tokens[10]), float.Parse(tokens[11]));

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static bool AssignTransform(GameObject obj, GameObject refObject, string data, bool preserveScale = false)
        {
            if (data == null) return false;
            string[] tokens = data.Split(',');

            try
            {
                var locToWorld = refObject.transform.localToWorldMatrix;
                obj.transform.position = locToWorld.MultiplyPoint(new Vector3(float.Parse(tokens[2]), float.Parse(tokens[3]), float.Parse(tokens[4])));


                obj.transform.localRotation = new Quaternion(float.Parse(tokens[5]), float.Parse(tokens[6]), float.Parse(tokens[7]), float.Parse(tokens[8]));
                if (!preserveScale) obj.transform.localScale = new Vector3(float.Parse(tokens[9]), float.Parse(tokens[10]), float.Parse(tokens[11]));

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        #endregion

        #region UNITY_METHODS
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            state = LoggingState.IDLE;
            lastState = state;

            nameToComponentObj = new Dictionary<string, List<ObjectLogger>>();
            loggers = new List<ObjectLogger>();

            if (registeredObjects != null)
            {
                foreach (GameObject obj in registeredObjects)
                {
                    List<ObjectLogger> list = new List<ObjectLogger>();
                    nameToComponentObj.Add(obj.name, list);
                    foreach (var c in obj.GetComponents<ObjectLogger>())
                    {
                        list.Add(c);
                        loggers.Add(c);
                    }
                }
            }

            frameNum = 0;
            frameTime = 0;
        }
        void Start()
        {
            var avatarMeshes = avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var mesh in avatarMeshes)
            {
                mesh.enabled = false;
            }
        }
        private void Update()
        {
            if (state != lastState) // reset counter and timer on new state run
            {

                lastState = state;
                // make sure if it's called last object in the tree or the first (depend on recording or playback) 
                if (this.IsRecording)
                {
                    frameNum = 0;
                    frameTime = 0;
                    recordedFramesListIdx = 0;
                    this.transform.parent = null;
                    this.transform.SetAsLastSibling();
                    //avatar control
                    HeadTracking.trackHeadBody = true;
                    rightHandTracking.trackHand = true;
                    leftHandTracking.trackHand = true;
                }
                else if (this.IsPlaying)
                {
                    this.transform.parent = null;
                    this.transform.SetAsFirstSibling();
                    //avatar control
                    HeadTracking.trackHeadBody = false;
                    rightHandTracking.trackHand = false;
                    leftHandTracking.trackHand = false;
                }
                if (showAvatar && (IsPlaying || IsPaused))
                {
                    var avatarMeshes = avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var mesh in avatarMeshes)
                    {
                        mesh.enabled = true;
                    }
                    avatarModel.gameObject.SetActive(true);
                }
                else
                {
                    var avatarMeshes = avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var mesh in avatarMeshes)
                    {
                        mesh.enabled = false;
                    }
                }
            }
            if (this.IsPlaying)
            {
                try
                {
                    RunOnce();
                }
                finally
                {
                    //Update frame num and time
                    //frameNum++;
                    frameTime += Time.unscaledDeltaTime;
                }
            }
        }
        // This function runs once per frame, and is used to replay the recorded data to an exact frame
        private void RunOnce()
        {
            //print("FrameNum:" + frameNum + ", FrameTime: " + frameTime);
            if (frameNum == 0)
            {
                ReplayData(0, updatesOnEachFrame);
                recordedFramesListIdx = 1;
            }

            //Run all recorded frame that should be done on or before this frame time (synced)
            while (recordedFramesListIdx < recordedFramesList.Count && GetRecordedFrameTime(recordedFramesList[recordedFramesListIdx]) <= frameTime)
            {
                this.frameNum = recordedFramesList[recordedFramesListIdx];
                recordedFramesListIdx++;

                if (this.frameNum > this.pauseFrame)
                {
                    Pause();

                    break;
                }
                ReplayData(frameNum, updatesOnEachFrame);
            }
            if (recordedFramesListIdx >= recordedFramesList.Count && this.frameTime >= this.MetaInfo.lengthOfClip)
            {
                Pause();
            }
            if (this.frameTime >= this.pauseTime) // stop on the target frame or time (as maybe not be a recorded frame on that time)
            {
                Pause();
            }
        }
        private void LateUpdate()
        {
            if (this.IsRecording)
            {
                try
                {

                    if (frameNum == 0)
                    {
                        EWManager.Log($"Recording scene in file: {filePath}");
                        foreach (ObjectLogger obj in loggers)
                        {
                            obj.ResetChangeTrackers();
                        }
                    }

                    string data = GetNewData();
                    if (data != null && data.Length > 0)
                    {
                        if (frameNum == 0)
                        {
                            File.WriteAllText(filePath, data);
                        }
                        else
                        {
                            File.AppendAllText(filePath, data);
                        }
                    }
                }
                finally
                {
                    //Update frame num and time
                    frameNum++;
                    frameTime += Time.unscaledDeltaTime;
                }
            }
        }
        #endregion

        #region FUNCTIONALITY
        private string GetNewData()
        {
            if (registeredObjects == null) return string.Empty;
            string allNewLogs = string.Empty;

            foreach (ObjectLogger obj in loggers)
            {
                string temp = obj.Fetch(frameNum);
                if (temp.Length > 0)
                {
                    if (temp.EndsWith("\n"))
                    {
                        allNewLogs += obj.GetName() + "," + temp;
                    }
                    else
                    {
                        allNewLogs += obj.GetName() + "," + temp + "\n";
                    }
                }
            }

            if (allNewLogs.Length > 0)
            {
                allNewLogs = frameNum + "," + frameTime + "\n" + allNewLogs;
            }
            return allNewLogs;
        }

        private void ReplayData(int frameNum, Dictionary<int, string[][]> frameData)
        {
            if (frameData == null) return;
            string[][] value;
            if (frameData.TryGetValue(frameNum, out value))
            {
                if (value != null)
                {
                    lastObjectsName = new List<string>();
                    foreach (string[] update in value) // Line by line
                    {
                        if (update.Length > 1)
                        {
                            List<ObjectLogger> objs = nameToComponentObj[update[0]];
                            if (objs != null && objs.Count > 0)
                            {
                                lastObjectsName.Add(update[0]);
                                foreach (ObjectLogger obj in objs)
                                {
                                    obj.Call(update);
                                }
                            }
                        }
                    }
                }
            }
        }

        private float GetRecordedFrameTime(int frameNum)
        {
            //if (frameData == null) throw new ArgumentNullException("Frame data is null!");
            string[][] value;
            float recordedTime;
            if (updatesOnEachFrame.TryGetValue(frameNum, out value))
            {
                if (value != null)
                {
                    if (value[0].Length == 1 && float.TryParse(value[0][0], out recordedTime)) //Recorded frame time should be at first element and is just a one-element string array
                    {
                        return recordedTime;
                    }
                    else
                    {
                        throw new Exception("Recorded file is not well-formatted!");
                    }
                }
            }
            return float.MinValue;
        }

        /// <summary>
        /// Estimates the time corresponding to the frame number.
        /// if there is an item for that frame, it returns the exact time (not estimated) by GetRecordedFrameTime.
        /// </summary>
        /// <param name="frameNum"></param>
        /// <returns></returns>
        private float GetEstimatedRecordedFrameTime(int frameNum)
        {
            float recordedTime = GetRecordedFrameTime(frameNum);
            if (recordedTime >= 0) return recordedTime;
            int listCount = recordedFramesList.Count;
            if (listCount == 0) return float.MinValue;
            if (listCount == 1)
            {
                return (this.MetaInfo.lengthOfClip / this.MetaInfo.framesCount) * frameNum;
            }
            int prevFrame = 0, nextFrame = 0;
            float prevTime = 0, nextTime = 0;
            if (listCount >= 2)
            {
                prevFrame = recordedFramesList[listCount - 2];
                nextFrame = recordedFramesList[listCount - 1];
                prevTime = GetRecordedFrameTime(prevFrame);
                nextTime = GetRecordedFrameTime(nextFrame);
            }
            EWManager.Log("Frame Num between: " + frameNum);
            for (int i = 1; i < listCount; i++)
            {
                if (recordedFramesList[i - 1] < frameNum && recordedFramesList[i] > frameNum)
                {
                    prevFrame = recordedFramesList[i - 1];
                    nextFrame = recordedFramesList[i];

                    prevTime = GetRecordedFrameTime(prevFrame);
                    nextTime = GetRecordedFrameTime(nextFrame);
                    break;
                }
            }
            recordedTime = (frameNum - prevFrame) / ((float)(nextFrame - prevFrame));
            recordedTime = recordedTime * (nextTime - prevTime) + prevTime;
            if (recordedTime < prevTime || recordedTime > nextTime)
            {
                EWManager.Error("Wrong Calcualtion, rec:" + recordedTime + ", prev:" + prevTime + ", next:" + nextTime + ", preFrame:" + prevFrame + ", nexFrame:" + nextFrame);
            }
            else
            {
                EWManager.Log("Wrong Calcualtion, rec:" + recordedTime + ", prev:" + prevTime + ", next:" + nextTime + ", preFrame:" + prevFrame + ", nexFrame:" + nextFrame);
            }
            return recordedTime;
        }

        private void ReadAllDataToDic()
        {
            try
            {
                List<string[]> loadedData = new List<string[]>();
                updatesOnEachFrame = new Dictionary<int, string[][]>();
                recordedFramesList = new List<int>();
                int frNum = 0, lastFrNum = -1;
                List<string[]> tempStrArray = null;
                string[] data = File.ReadAllLines(filePath);
                if (data.Length > 0)
                {
                    //Tokenise Data
                    foreach (string str in data)
                    {
                        loadedData.Add(str.Split(','));
                    }
                    foreach (string[] strs in loadedData)
                    {
                        try
                        {
                            frNum = Convert.ToInt32(strs[0]);
                            if (frNum > lastFrNum)
                            {
                                if (tempStrArray != null)
                                {
                                    updatesOnEachFrame.Add(lastFrNum, tempStrArray.ToArray());
                                    recordedFramesList.Add(lastFrNum);
                                }
                                tempStrArray = new List<string[]>
                                {
                                    new string[] { strs[1] }
                                };
                                lastFrNum = frNum;
                            }
                            else
                            {
                                EWManager.Error("Frames are not sorted in correct order!");
                            }
                        }
                        catch (Exception)
                        {
                            tempStrArray.Add(strs);
                        }
                    }
                    if (tempStrArray != null && tempStrArray.Count > 0)
                    {
                        updatesOnEachFrame.Add(lastFrNum, tempStrArray.ToArray());
                        recordedFramesList.Add(lastFrNum);
                    }
                }
            }
            catch (Exception)
            {
                updatesOnEachFrame = null;
                recordedFramesList = null;
                EWManager.Error("Error in reading or parsing recorded scene data file!");
            }
        }
        #endregion
    }
}