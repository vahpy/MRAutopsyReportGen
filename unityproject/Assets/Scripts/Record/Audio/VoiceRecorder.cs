using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace HoloAutopsy.Record.Audio
{
    public class VoiceRecorder : MonoBehaviour
    {
        // Inspector fields

        [SerializeField]
        private string[] devices = null;

        [SerializeField]
        private string deviceName = "";

        [SerializeField]
        private bool isDeviceSelected = false;

        [Header("Event Listeners")]
        [SerializeField]
        private UnityEvent startEvents;
        [SerializeField]
        private UnityEvent saveEvents;
        [SerializeField]
        private UnityEvent playEvents;



        // private fields
        private string outputFilePath = string.Empty;
        private AudioSource audioSource;

        private bool isSaved = false;
        private bool doSave = false;
        private int lastSample = 0;
        public bool IsRecording { private set; get; } = false;
        private float currentStartRecordTime = 0;

        private string _lastLoadedFile = string.Empty;
        #region PUBLIC_API
        // property-like method
        public bool IsPlaying
        {
            get
            {
                if (audioSource == null) return false;
                return audioSource.isPlaying;
            }
        }

        public string GetSavedFileAbsolutePath()
        {
            return outputFilePath;
        }
        public void SelectDevice(int deviceNum)
        {
            try
            {
                deviceName = devices[deviceNum];
                isDeviceSelected = true;
            }
            catch (Exception)
            {
                EWManager.Warning("Not found the requested device!");
            }
        }
        public bool Record()
        {
            if (isDeviceSelected)
            {
                return StartRecording();
            }
            else
            {
                EWManager.Error("Input Device (Microphone) is not selected!");
                return false;
            }
        }
        public bool StopSave(string file)
        {
            this.outputFilePath = file;
            doSave = true;
            return true;
        }

        public bool LoadPlay(string filePath, float time)
        {
            return PlayFile(filePath, time);
        }

        public void StopPlaying()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        public float GetClipLength()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                return audioSource.clip.length;
            }
            return 0;
        }
        #endregion

        #region UNITY_METHODS
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            devices = Microphone.devices;

            if (devices.Length > 0)
            {
                //prioritize microphone name
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].ToLower().Contains("jbl") || devices[i].ToLower().Contains("li"))
                    {
                        deviceName = devices[i];
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(deviceName)) deviceName = devices[devices.Length - 1];
                isDeviceSelected = true;
            }
        }
        private void Update()
        {
            //Save on coroutine
            if (doSave)
            {
                doSave = false;
                if (IsRecording)
                {
                    StopRecording();
                    StartCoroutine(SaveClip());
                }
                else
                {
                    EWManager.Warning("There is no recording voice to be stopped or saved!");
                }
            }
        }
        #endregion

        #region FUNCTIONALITY
        // Microphone
        private bool StartRecording()
        {
            if (IsRecording || doSave) return false;

            try
            {
                if (audioSource.clip == null)
                {
                    audioSource.clip = Microphone.Start(deviceName, true, 5 * 60, 16000);//44100); // Maximum 2 mins
                    audioSource.loop = false;
                    currentStartRecordTime = Time.time;

                }
                if (audioSource.clip == null)
                {
                    EWManager.Error($"Microphone \"{deviceName}\" has not been found");
                    return false;
                }
                IsRecording = true;
                isSaved = false;

                EWManager.Log("Start recording audio");

                if (startEvents != null) startEvents.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                EWManager.Error("Couldn't start recording!\n" + ex.ToString());
                return false;
            }
        }
        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;
            lastSample = Microphone.GetPosition(deviceName);
            Microphone.End(deviceName);
            audioSource.loop = false;
        }

        // File control:
        private bool DeleteClip(string filePath)
        {
            audioSource.clip = null;
            try
            {
                File.Delete(filePath);
                EWManager.Confirm("Deleted audio file: " + filePath);
            }
            catch (Exception)
            {
                EWManager.Error("Couldn't delete audio file successfully!");
                return false;
            }
            return true;
        }

        private IEnumerator SaveClip()
        {

            if (isSaved) yield break;// false;
            isSaved = true;
            //Todo: if the recording is larger than audio clip capacity, need to trim, merge, etc.
            // trim audio, possible minor issue: not capuring the last sample
            AudioClip recordedClip = audioSource.clip;
            if (recordedClip == null)
            {
                EWManager.Error("recorded clip is null");
                yield break;
            }
            
            float[] data = new float[recordedClip.samples * recordedClip.channels]; //900 /* seconds */ * 2 /* channels */ * 44100 /* sample rate */
            recordedClip.GetData(data, 0);


            bool isDone = false;
            byte[] wavFile = null;
            float[] samples = new float[lastSample * recordedClip.channels];
            for (int i = 0; i < lastSample; i++)
            {
                samples[i] = data[i];
            }

            //audioSource.clip = AudioClip.Create(recordedClip.name, lastSample, recordedClip.channels, recordedClip.frequency, false);
            //audioSource.clip.SetData(newData, 0);
            // Clip content:
            int channels = recordedClip.channels;
            int frequency = recordedClip.frequency;
            int sampleNum = recordedClip.samples;

            audioSource.clip = null;
            new Task(() =>
            {
                try
                {
                    wavFile = OpenWavParser.NewAudioClipDataToByteArray(samples, sampleNum, channels, frequency);
                    //wavFile = OpenWavParser.AudioClipToByteArray(audioSource.clip);
                    //print("Conversion done.");
                }
                catch (Exception ex) { print("Exception: "+ex.Message); }
                isDone = true;
            }).Start();
            int conversionTimeout = 100;
            for (int i = 0; i < conversionTimeout; i++)
            {
                if (isDone) break;
                //print("Still converting...");
                yield return new WaitForSeconds(1.0f);
            }
            if (wavFile == null)
            {
                EWManager.Error("Couldn't convert audio clip to wav format!");
                yield break;
            }

            using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Append))
            {
                for (int offset = 0; offset < wavFile.Length; offset += 50000)
                {
                    //print("OFFSET: " + offset);
                    int writingLength = Mathf.Min(50000, wavFile.Length - offset);
                    fileStream.Write(wavFile, offset, writingLength);
                    yield return null;
                }
            }
            

            EWManager.Confirm("Saved audio file at: " + outputFilePath);
            saveEvents?.Invoke();
        }

        private bool PlayFile(string filePath, float time)
        {
            if (IsRecording)
            {
                EWManager.Warning("Cannot play voice while it is recording!");
                return false;
            }
            if (audioSource.isPlaying)
            {
                EWManager.Warning("Another File is playing right now!");
                return false;
            }
            if (File.Exists(filePath))
            {
                if (!_lastLoadedFile.Equals(filePath))
                {
                    EWManager.Log("Playing: " + filePath);
                    byte[] wavFile = File.ReadAllBytes(filePath);
                    audioSource.clip = OpenWavParser.ByteArrayToAudioClip(wavFile);
                    _lastLoadedFile = filePath;
                }
                audioSource.volume = 1;
                if (time > 0)
                {
                    audioSource.time = time;
                }
                if (audioSource.clip.length > time)
                {
                    audioSource.Play();
                    if (playEvents != null) playEvents.Invoke();
                    return true;
                }
                else
                {
                    EWManager.Warning("An invalid seek position was passed to the audio source.");
                    return false;
                }
            }
            else
            {
                EWManager.Warning("File not found: " + filePath);
                return false;
            }
        }
        #endregion
    }
}