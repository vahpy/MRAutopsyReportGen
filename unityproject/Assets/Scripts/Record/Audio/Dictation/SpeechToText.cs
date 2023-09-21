using UnityEngine;
using UnityEngine.Windows.Speech;

namespace HoloAutopsy.Record.Audio
{
    //[ExecuteInEditMode]
    public class SpeechToText : MonoBehaviour, SpeechToTextInf
    {
        [SerializeField] private bool debugBtn = true;
        private bool phraseRecognitionPreviousStatus = false;

        private bool isReadOnly = false;

        private string filePath = string.Empty;

        DictationRecognizer dictation;

        [Header("Event Listeners")]
        [SerializeField]
        TextContainerEvent hypothesisTxtListeners;
        [SerializeField]
        TextContainerEvent resultTxtListeners;
        [SerializeField]
        TextContainerEvent errorTxtListeners;
        [SerializeField]
        TextContainerEvent statusListeners;
        [SerializeField]
        TextContainerEvent resultTxtMultiDeviceListeners;

        #region PUBLIC_API
        public bool IsTranscribing()
        {
            return dictation != null;
        }
        /// <summary>
        /// To start dictation, first initialize and then start and stop as many as you want, until close the file!
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dialogueUI"></param>
        /// <param name="isReadOnly"></param>
        public void Initialize(string filePath, SpeechTextDialogue dialogueUI, bool isReadOnly)
        {
            phraseRecognitionPreviousStatus = false;
            this.isReadOnly = isReadOnly;
            this.filePath = filePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                EWManager.Warning("No file path set");
                return;
            }
            if (isReadOnly)
            {
                this.filePath = filePath;
                LoadFile(dialogueUI);
                return;
            }

            SetNewUIListeners(dialogueUI);

            if (hypothesisTxtListeners == null && resultTxtListeners == null)
            {
                EWManager.Warning("App Bug: There is no listener for showing speech to text! Still saved to the file.");
            }
            if (dictation != null)
            {
                this.StopDictation();
                dictation.Dispose();
                dictation = null;
            }

            dictation = new DictationRecognizer();
            dictation.DictationHypothesis += (text) =>
            {
                hypothesisTxtListeners?.Invoke(ProcessPhrase(text));
                statusListeners?.Invoke("Listening");
            };

            dictation.DictationResult += (text, confidence) =>
            {
                var processedText = ProcessPhrase(text);
                AppendToFile(processedText);
                resultTxtListeners?.Invoke(processedText);
                statusListeners?.Invoke("Listening");
                resultTxtMultiDeviceListeners?.Invoke(processedText);
            };
            dictation.DictationComplete += (cause) =>
            {
                this.StopDictation();
                this.StartDictation();
                errorTxtListeners?.Invoke("C:" + cause.ToString());
                statusListeners?.Invoke(cause.ToString());
            };
            dictation.DictationError += (error, hres) =>
            {
                errorTxtListeners?.Invoke("E:" + error + " and " + hres);
                statusListeners?.Invoke(error.ToString());
            };
        }

        private void SetNewUIListeners(SpeechTextDialogue dialogue)
        {
            if (dialogue == null) return;
            hypothesisTxtListeners?.RemoveAllListeners();
            hypothesisTxtListeners = new TextContainerEvent();
            hypothesisTxtListeners.AddListener(dialogue.NewHypothesisString);

            resultTxtListeners?.RemoveAllListeners();
            resultTxtListeners = new TextContainerEvent();
            resultTxtListeners.AddListener(dialogue.NewResultString);

            errorTxtListeners?.RemoveAllListeners();
            errorTxtListeners = new TextContainerEvent();
            errorTxtListeners.AddListener(dialogue.NewErrorString);

            statusListeners?.RemoveAllListeners();
            statusListeners = new TextContainerEvent();
            statusListeners.AddListener(dialogue.NewStatusMsg);
        }
        public void CloseFile()
        {
            StopDictation();
            EWManager.Confirm("Saved transcibe file at: " + this.filePath);
            isReadOnly = true;
        }
        public void LoadFile(SpeechTextDialogue dialogue)
        {
            try
            {
                var recordedText = System.IO.File.ReadAllText(this.filePath);
                dialogue?.NewResultString(recordedText);
            }
            catch (System.Exception)
            {
                EWManager.Warning("Error in reading from the transcription file! File Path: " + filePath);
            }
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

        public void StartDictation()
        {
            if (isReadOnly) return;
            if (dictation == null)
            {
                EWManager.Warning("App Bug: Need to initialize before start/stop dictation!");
                return;
            }
            if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
            {
                phraseRecognitionPreviousStatus = true;
                PhraseRecognitionSystem.Shutdown();
            }

            dictation.Start();
            statusListeners?.Invoke("Starting");
        }

        public void StopDictation()
        {
            if (isReadOnly) return;
            if (dictation == null)
            {
                EWManager.Warning("App Bug: Need to initialize before start/stop dictation!");
            }
            dictation?.Stop();
            dictation?.Dispose();
            dictation = null;
            statusListeners?.Invoke("Idle");

            if (phraseRecognitionPreviousStatus)
            {
                phraseRecognitionPreviousStatus = true;
                if (PhraseRecognitionSystem.Status != SpeechSystemStatus.Running)
                {
                    PhraseRecognitionSystem.Restart();
                }
            }

        }
        #endregion
        #region FUNCIONALITY
        private string ProcessPhrase(string phrase)
        {

            if (phrase != null)
            {
                if (phrase.ToLower().Trim().EndsWith("dot"))
                {
                    int index = phrase.ToLower().LastIndexOf("dot");
                    phrase = phrase.Substring(0, index) + ".";
                }
                if (phrase.ToLower().Trim().EndsWith("comma"))
                {
                    int index = phrase.ToLower().LastIndexOf("comma");
                    phrase = phrase.Substring(0, index) + ",";
                }
                return phrase + " ";
            }
            else
            {
                return "";
            }
        }
        #endregion

        //debugging
        private void Update()
        {
            if(!debugBtn)
            {
                string processedText = "N" + Random.Range(0, 1);
                resultTxtMultiDeviceListeners?.Invoke(processedText);
                debugBtn = true;
            }
        }

    }
}