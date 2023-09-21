using HoloAutopsy.Record.Audio;
using HoloAutopsy.Record.Logging;
using Photon.Pun.Demo.Cockpit;
using System;
using System.IO;
using UnityEngine;

namespace HoloAutopsy.Record
{
    public class RecordingBubbleManager : MonoBehaviour
    {
        [SerializeField]
        private bool recordAudio = true;
        [SerializeField]
        private bool recordScene = true;
        [SerializeField]
        private bool recordTranscibe = true;
        [SerializeField]
        private VoiceRecorder audioRecorder;

        [SerializeField]
        private LoggingManager loggingManager;

        [SerializeField]
        private GameObject speechToTxtManagerObject = default;
        private SpeechToTextInf speechToTxtManager;

        [SerializeField]
        private GameObject bubblePrefab = default;

        [SerializeField]
        private RecordBubble currBubble = null;


        private string audioFilePath;
        private string sceneLogFilePath;
        private string transcribeFilePath;
        private float startRecordingSinceBegin;

        private void OnEnable()
        {
            speechToTxtManager = null;
            if (speechToTxtManagerObject != null)
            {
                var spComps = speechToTxtManagerObject.GetComponents<SpeechToTextInf>();
                foreach (var sp in spComps)
                {
                    MonoBehaviour spMono = (MonoBehaviour)sp; // All SpeechToTextManagers should inheret MonoBehavior as well
                    if (spMono.enabled)
                    {
                        speechToTxtManager = sp; //first active speech to text manager is used
                        break;
                    }
                }
            }
            //EWManager.Log("Voice transcription by \""+(speechToTxtManager?.GetType()?.Name)+"\"");
        }

        private void Update()
        {
            if (currBubble != null)
            {
                if (audioRecorder.IsPlaying || loggingManager.IsPlaying) currBubble.state = 2;
                else if (audioRecorder.IsRecording || loggingManager.IsRecording) currBubble.state = 1;
                else
                {
                    currBubble.state = 0;
                }
            }
        }
        /// <summary>
        /// Once triggered it starts recording, second time it stops the recording and save file
        /// </summary>
        public void RecordPressed()
        {
            if (audioRecorder.IsPlaying || loggingManager.IsPlaying)
            {
                EWManager.Warning("Cannot record while playing!");
                return;
            }

            if (audioRecorder.IsRecording || loggingManager.IsRecording || speechToTxtManager.IsTranscribing())
            {
                //Save recorded audio and/or scene
                EndRecording();
            }
            else
            {
                //Start Recording
                StartRecording();
            }
        }

        private void StartRecording()
        {
            bool audioSucc = false, sceneSucc = false;

            DateTime timeNow = DateTime.Now;
            float frameTime = Time.time;
            string suffix = timeNow.ToString("HH,mm,ss") + "-" + frameTime;
            string startDate = timeNow.ToString("yyyy/MM/dd");
            string startTime = timeNow.ToString("HH:mm:ss");
            if (recordScene)
            {
                sceneLogFilePath = Path.Combine(Application.persistentDataPath, "SCN-" + suffix + ".txt");
                sceneSucc = loggingManager.RecordAt(sceneLogFilePath, startDate, startTime);
            }
            if (recordAudio)
            {
                audioFilePath = Path.Combine(Application.persistentDataPath, "AUD-" + suffix + ".wav");
                audioSucc = audioRecorder.Record();
            }
            if (recordTranscibe)
            {
                transcribeFilePath = Path.Combine(Application.persistentDataPath, "TXT-" + suffix + ".txt");
            }

            if ((audioSucc || !recordAudio) && (sceneSucc || !recordScene))
            {
                //Create Record Bubble
                currBubble = Instantiate(bubblePrefab, this.transform).GetComponent<RecordBubble>();
                currBubble.transform.position = Camera.main.transform.position + Camera.main.transform.forward + new Vector3(0, -0.2f, 0);
                currBubble.actualPosition = currBubble.transform.localPosition;
                currBubble.state = 1;
                currBubble.frameTime = frameTime;
                currBubble.realStartDate = startDate;
                currBubble.realStartTime = startTime;
                currBubble.audioFilePath = audioFilePath;
                currBubble.sceneLogFilePath = sceneLogFilePath;
                currBubble.lengthOfClip = 0;
                startRecordingSinceBegin = Time.fixedTime;

                currBubble.transcibeFilePath = transcribeFilePath;

                currBubble.RefreshUI();

                //Play success sound
                EWManager.Confirm(null);
            }
            else
            {
                if (audioSucc)
                {
                    audioRecorder.StopRecording();
                }
                if (sceneSucc)
                {
                    loggingManager.StopRecording();
                }
                if (speechToTxtManager != null)
                {
                    speechToTxtManager.StopDictation();
                    speechToTxtManager.CloseFile();
                }
                if (currBubble != null) Destroy(currBubble);
                EWManager.Error("Couldn't start recording properly! Audio recording: " + audioSucc + ", Scene Recording: " + sceneSucc);
            }
            if (recordTranscibe)
            {
                speechToTxtManager.Initialize(transcribeFilePath, currBubble?.GetComponentInChildren<SpeechTextDialogue>(), false);
                speechToTxtManager.StartDictation();
            }
        }

        private void EndRecording()
        {
            bool audioSucc = false, sceneSucc = false;

            if (currBubble != null)
            {
                //revise the code below later
                //currBubble.lengthOfClip = Time.fixedTime - startRecordingSinceBegin;
                currBubble.lengthOfClip = LoggingManager.Instance.frameTime;
                currBubble.framesCount = LoggingManager.Instance.frameNum;
            }

            if (recordAudio)
            {
                audioSucc = audioRecorder.StopSave(audioFilePath);
            }
            if (recordScene) sceneSucc = loggingManager.StopRecording();
            if (recordTranscibe)
            {
                speechToTxtManager.StopDictation();
                speechToTxtManager.CloseFile();
            }

            if ((audioSucc || !recordAudio) && (sceneSucc || !recordScene))
            {
                //Change recording state to idle state
                if (currBubble != null)
                {
                    var c = currBubble;
                    RecordFileMetaInfo recMetaInfo = new RecordFileMetaInfo(c.audioFilePath, c.sceneLogFilePath, c.transcibeFilePath, c.realStartDate, c.realStartTime, c.frameTime, c.lengthOfClip, c.framesCount, c.actualPosition);
                    //RecordedFileManager.Instance.AddNewFiles(c.realStartDate, c.realStartTime, c.frameTime, c.lengthOfClip, c.actualPosition, c.audioFilePath, c.sceneLogFilePath, c.transcibeFilePath);
                    RecordedFileManager.Instance.AddNewMetaFile(recMetaInfo);
                    c.state = 0;
                }
                //Play success sound
                EWManager.Confirm(null);
            }
            else
            {
                if (currBubble != null)
                {
                    if (audioSucc || sceneSucc)
                    {
                        var c = currBubble;
                        //RecordedFileManager.Instance.AddNewFiles(c.realStartDate, c.realStartTime, c.frameTime, c.lengthOfClip,
                        //    c.transform.localPosition,
                        //    (audioSucc ? c.audioFilePath : string.Empty), (sceneSucc ? c.sceneLogFilePath : string.Empty), c.transcibeFilePath);
                        RecordFileMetaInfo recMetaInfo = new RecordFileMetaInfo((audioSucc ? c.audioFilePath : string.Empty), (sceneSucc ? c.sceneLogFilePath : string.Empty),
                            c.transcibeFilePath, c.realStartDate, c.realStartTime, c.frameTime, c.lengthOfClip, c.framesCount, c.actualPosition);
                        RecordedFileManager.Instance.AddNewMetaFile(recMetaInfo);
                    }
                    Destroy(currBubble);
                }
                EWManager.Error("Couldn't save audio or scene properly! Audio recording: " + audioSucc + ", Scene Recording: " + sceneSucc + "\n Please check these files `" + audioFilePath + "` and `" + sceneLogFilePath + "`");
            }
        }

        public void PlayPressed()
        {
            if (currBubble == null)
            {
                EWManager.Warning("No recorded bubble selected!");
                return;
            }
            try
            {
                if (audioRecorder.IsRecording || loggingManager.IsRecording)
                {
                    EWManager.Warning("Cannot play while recording!");
                    return;
                }
                audioRecorder.StopPlaying();
                loggingManager.StopPlaying();
                loggingManager.LoadPlay(currBubble.sceneLogFilePath);
                audioRecorder.LoadPlay(currBubble.audioFilePath, 0);
                currBubble.state = 2;
                EWManager.Confirm();
            }
            catch (Exception)
            {
                audioRecorder.StopPlaying();
                loggingManager.StopPlaying();
                EWManager.Error("Couldn't load and play files `" + currBubble.audioFilePath + "` and/or `" + currBubble.sceneLogFilePath + "`");
            }
        }

        public void StopPlaying()
        {
            audioRecorder.StopPlaying();
            loggingManager.StopPlaying();
        }

        public void SelectRecordBubble(GameObject selectObj)
        {
            if (selectObj.GetComponent<RecordBubble>() != null)
            {
                if (currBubble != null)
                {
                    StopPlaying();
                    currBubble.state = 0;
                }
                currBubble = selectObj.GetComponent<RecordBubble>();
            }
        }

        //public void AddBubble(string audioFile, string sceneFile, string transcribeFile, string realDate, string realTime, float frameTime, float lengthOfClip, int framesCount, Vector3 position)
        //{
        //    currBubble = Instantiate(bubblePrefab, this.transform).GetComponent<RecordBubble>();
        //    currBubble.transform.localPosition = position;
        //    currBubble.actualPosition = position;
        //    currBubble.realStartDate = realDate;
        //    currBubble.realStartTime = realTime;
        //    currBubble.frameTime = frameTime;
        //    currBubble.lengthOfClip = lengthOfClip;
        //    currBubble.framesCount = framesCount;
        //    currBubble.audioFilePath = audioFile;
        //    currBubble.sceneLogFilePath = sceneFile;
        //    currBubble.transcibeFilePath = transcribeFile;
        //    speechToTxtManager.Initialize(transcribeFile, currBubble.GetComponentInChildren<SpeechTextDialogue>(), true);
        //    currBubble.RefreshUI();
        //}
        public void AddBubble(RecordFileMetaInfo metaInfo)
        {
            currBubble = Instantiate(bubblePrefab, this.transform).GetComponent<RecordBubble>();
            currBubble.transform.localPosition = metaInfo.position;
            currBubble.actualPosition = metaInfo.position;
            currBubble.realStartDate = metaInfo.realDate;
            currBubble.realStartTime = metaInfo.realTime;
            currBubble.frameTime = metaInfo.frameTime;
            currBubble.lengthOfClip = metaInfo.lengthOfClip;
            currBubble.framesCount = metaInfo.framesCount;
            currBubble.audioFilePath = metaInfo.audioFile;
            currBubble.sceneLogFilePath = metaInfo.sceneFile;
            currBubble.transcibeFilePath = metaInfo.transcribeFile;
            speechToTxtManager.Initialize(metaInfo.transcribeFile, currBubble.GetComponentInChildren<SpeechTextDialogue>(), true);
            currBubble.RefreshUI();
        }

    }
    public interface SpeechToTextInf
    {
        public bool IsTranscribing();
        public void StartDictation();
        public void StopDictation();
        public void CloseFile();

        public void Initialize(string filePath, SpeechTextDialogue dialogueUI, bool isReadOnly);
    }
}