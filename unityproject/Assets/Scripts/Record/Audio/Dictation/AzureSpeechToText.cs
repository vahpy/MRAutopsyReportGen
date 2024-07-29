using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using UnityEditor.DeviceSimulation;
using UnityEngine.XR;
using HoloAutopsy.Record.Audio;
using HoloAutopsy.Record;
//using static Unity.Collections.NativeArray;
using UnityEngine.Windows.Speech;
using System.Threading;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
#if PLATFORM_IOS
using UnityEngine.iOS;
using System.Collections;
#endif
namespace HoloAutopsy.Record.Audio
{
    public class AzureSpeechToText : MonoBehaviour, SpeechToTextInf
    {
        [SerializeField]
        private string[] devices;
        [SerializeField]
        private string selectedMic = default;
        [SerializeField]
        private EchoServerBehavior server = default;
        [Header("Event Listeners")]
        [SerializeField]
        TextContainerEvent recognizingTxtListeners;
        [SerializeField]
        TextContainerEvent recognizedTxtListeners;
        [SerializeField]
        TextContainerEvent errorTxtListeners;
        [SerializeField]
        TextContainerEvent statusListeners;
        
        [SerializeField]
        private AudioSource audioSource = default;

        private bool micPermissionGranted = false;

        SpeechRecognizer recognizer;
        SpeechConfig config;
        AudioConfig audioInput;
        PushAudioInputStream pushStream;

        private object threadLocker = new object();
        private bool recognitionStarted = false;

        int lastSample = 0;
        private bool isReadOnly = false;

        private string filePath = string.Empty;

        //message passing static fields
        private string recognizingTxt;
        private string recognizedTxt;
        private string canceledTxt;

#if PLATFORM_ANDROID || PLATFORM_IOS
    // Required to manifest microphone permission, cf.
    // https://docs.unity3d.com/Manual/android-manifest.html
    private Microphone mic;
#endif

        private byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
        {
            MemoryStream dataStream = new MemoryStream();
            int x = sizeof(Int16);
            Int16 maxValue = Int16.MaxValue;
            int i = 0;
            while (i < data.Length)
            {
                dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
                ++i;
            }
            byte[] bytes = dataStream.ToArray();
            dataStream.Dispose();
            return bytes;
        }

        private void sendRecognizingMsg()
        {
            lock (threadLocker)
            {
                if (recognizingTxt != null && recognizingTxt.Trim().Length != 0)
                {
                    recognizingTxtListeners?.Invoke(recognizingTxt);
                    statusListeners?.Invoke("Listening");
                    recognizingTxt = null;
                }
            }
        }
        private void sendRecognizedMsg()
        {
            lock (threadLocker)
            {
                if (recognizedTxt != null && recognizedTxt.Trim().Length != 0)
                {
                    print("Recognized Text: " + recognizedTxt);
                    string tempStr = new string(recognizedTxt);
                    //print("RecognizedHandler fired:" + recognizedTxt);
                    recognizedTxtListeners?.Invoke(tempStr);
                    try
                    {
                        //print("Start sending to devices:"+(resultTxtMultiDeviceListeners==null?"null":"not null"));
                        print("Text to projector: " + tempStr);
                        print("Azurespeech: "+Thread.CurrentThread.ManagedThreadId);
                        server?.SendSpeechToTextMsg(tempStr);
                        //print("Sent to multi devices");
                    }catch(Exception ex)
                    {
                        print(ex.Message);
                    }
                    AppendToFile(tempStr);
                    statusListeners?.Invoke("Listening");
                    recognizedTxt = null;
                }
            }
        }
        private void sendCanceledMsg()
        {
            lock (threadLocker)
            {
                if (canceledTxt != null && canceledTxt.Trim().Length != 0)
                {
                    //print("CanceledHandler fired:" + canceledTxt);
                    errorTxtListeners?.Invoke(canceledTxt);
                    canceledTxt = null;
                }
            }
        }

        private void RecognizingHandler(object sender, SpeechRecognitionEventArgs e)
        {
            lock (threadLocker)
            {
                recognizingTxt = e.Result.Text;
            }
        }

        private void RecognizedHandler(object sender, SpeechRecognitionEventArgs e)
        {
            lock (threadLocker)
            {
                if (recognizedTxt != null)
                {
                    recognizedTxt += e.Result.Text;
                }
                else
                {
                    recognizedTxt = e.Result.Text;
                }
            }
        }

        private void CanceledHandler(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            lock (threadLocker)
            {
                if (canceledTxt != null)
                {
                    canceledTxt += e.ErrorDetails.ToString();
                }
                else
                {
                    canceledTxt = e.ErrorDetails.ToString();
                }
            }
        }

        private async void StopSpeechRecognition()
        {
            print("Speech Recognition Stoping");
            if (recognitionStarted)
            {
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(true);

                if (Microphone.IsRecording(selectedMic))
                {
                    Debug.Log("Microphone.End: " + selectedMic);
                    Microphone.End(null);
                    lastSample = 0;
                }

                lock (threadLocker)
                {
                    recognitionStarted = false;
                    Debug.Log("Recognition Stopped");
                }
            }
        }

        private async void StartSpeechRecognition()
        {
            print("Speech Recognition Starting");
            selectedMic = GameObject.Find("AudioRecManager").GetComponent<VoiceRecorder>().GetSelectedMicrophoneName();
            if (selectedMic == null || selectedMic.Trim().Length == 0)
            {
                selectedMic = Microphone.devices[0];
            }
            if (audioSource == null) audioSource = GameObject.Find("AudioRecManager").GetComponent<AudioSource>();

            if (!Microphone.IsRecording(selectedMic))
            {
                //if (audioSource.clip == null) // If voice is recorded, it's been assigned by voice recorder, otherwise, it starts for transciption
                //{
                    audioSource.clip = Microphone.Start(selectedMic, true, 5*60, 16000);
                    audioSource.loop = true;
                    Debug.Log("Started New Microphone");
                //}
                Debug.Log("Microphone.Start: " + selectedMic + ", audioSource.clip channels: " + audioSource.clip.channels + ", frequency:" + audioSource.clip.frequency);
            }

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            lock (threadLocker)
            {
                recognitionStarted = true;
                Debug.Log("Recognition Started");
            }
        }

        void Start()
        {
            // Continue with normal initialization, Text and Button objects are present.
#if PLATFORM_ANDROID
            // Request to use the microphone, cf.
            // https://docs.unity3d.com/Manual/android-RequestingPermissions.html
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#elif PLATFORM_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
#else
            micPermissionGranted = true;

#endif
            config = SpeechConfig.FromSubscription("", "australiaeast");
            pushStream = AudioInputStream.CreatePushStream();
            audioInput = AudioConfig.FromStreamInput(pushStream);

            recognizer = new SpeechRecognizer(config, audioInput);

            recognizer.Recognizing += RecognizingHandler;
            recognizer.Recognized += RecognizedHandler;
            recognizer.Canceled += CanceledHandler;

            // Microphone selection
            devices = new string[Microphone.devices.Length];
            int i = 0;
            foreach (var device in Microphone.devices)
            {
                //Debug.Log("DeviceName: " + device);
                devices[i] = device;
                i++;
            }
            recognizingTxt = null;
            recognizedTxt = null;
            canceledTxt = null;
        }

        void Disable()
        {
            recognizer.Recognizing -= RecognizingHandler;
            recognizer.Recognized -= RecognizedHandler;
            recognizer.Canceled -= CanceledHandler;
            pushStream.Close();
            recognizer.Dispose();
        }

        void FixedUpdate()
        {
#if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
            message = "Click button to recognize speech";
        }
#elif PLATFORM_IOS
        if (!micPermissionGranted && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            micPermissionGranted = true;
            message = "Click button to recognize speech";
        }
#endif

            if (Microphone.IsRecording(selectedMic) && recognitionStarted == true)
            {
                statusListeners?.Invoke("Listening");
                int pos = Microphone.GetPosition(selectedMic);
                int diff = pos - lastSample;

                if (diff > 0)
                {
                    float[] samples = new float[diff * audioSource.clip.channels];
                    audioSource.clip.GetData(samples, lastSample);
                    byte[] ba = ConvertAudioClipDataToInt16ByteArray(samples);
                    if (ba.Length != 0)
                    {
                        //Debug.Log("pushStream.Write pos:" + Microphone.GetPosition(selectedMic).ToString() + " length: " + ba.Length.ToString());
                        pushStream.Write(ba);
                    }
                }
                lastSample = pos;
            }
            else if (!Microphone.IsRecording(selectedMic) && recognitionStarted == false)
            {
                statusListeners?.Invoke("Idle1");
            }
            sendRecognizingMsg();
            sendRecognizedMsg();
            sendCanceledMsg();
        }

        public bool IsTranscribing()
        {
            return recognitionStarted;
        }

        public void StartDictation()
        {
            StartSpeechRecognition();
        }

        public void StopDictation()
        {
            StopSpeechRecognition();
        }
        public void AppendToFile(string content)
        {
            try
            {
                if (content != null)
                {
                    System.IO.File.AppendAllText(this.filePath, content);
                }
            }
            catch (System.Exception)
            {
                EWManager.Warning("Error in writing to the transcription file! File Path: " + filePath);
            }
        }
        public void CloseFile()
        {
            StopDictation();
            EWManager.Confirm("Saved transcibe file at: " + this.filePath);
            isReadOnly = true;
        }
        private void SetNewUIListeners(SpeechTextDialogue dialogue)
        {
            if (dialogue == null) return;
            recognizingTxtListeners?.RemoveAllListeners();
            recognizingTxtListeners = new TextContainerEvent();
            recognizingTxtListeners.AddListener(dialogue.NewHypothesisString);

            recognizedTxtListeners?.RemoveAllListeners();
            recognizedTxtListeners = new TextContainerEvent();
            recognizedTxtListeners.AddListener(dialogue.NewResultString);

            errorTxtListeners?.RemoveAllListeners();
            errorTxtListeners = new TextContainerEvent();
            errorTxtListeners.AddListener(dialogue.NewErrorString);

            statusListeners?.RemoveAllListeners();
            statusListeners = new TextContainerEvent();
            statusListeners.AddListener(dialogue.NewStatusMsg);
        }
        public void LoadFile(SpeechTextDialogue dialogue)
        {
            try
            {
                var recordedText = File.ReadAllText(this.filePath);
                dialogue?.NewResultString(recordedText);
            }
            catch (System.Exception)
            {
                EWManager.Warning("Error in reading from the transcription file! File Path: " + filePath);
            }
        }
        public void Initialize(string filePath, SpeechTextDialogue dialogueUI, bool isReadOnly)
        {

            this.isReadOnly = isReadOnly;
            this.filePath = filePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                EWManager.Warning("No file path set");
                return;
            }
            if (isReadOnly)
            {
                LoadFile(dialogueUI);
                return;
            }

            SetNewUIListeners(dialogueUI);

            if (recognizingTxtListeners == null && recognizedTxtListeners == null)
            {
                EWManager.Warning("App Bug: There is no listener for showing speech to text! Still saved to the file.");
            }

        }
    }
}
