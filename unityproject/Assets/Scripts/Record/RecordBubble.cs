
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace HoloAutopsy.Record
{
    public class RecordBubble : MonoBehaviour
    {
        /// <summary>
        /// Voice Bubble state: [0, idle],[1, recording], [2, playing]
        /// </summary>
        [HideInInspector]
        public int state;
        private int last_state;
        [SerializeField, HideInInspector]
        public string audioFilePath { internal set; get; } = string.Empty;
        [SerializeField, HideInInspector]
        public string sceneLogFilePath { internal set; get; } = string.Empty;

        [SerializeField, HideInInspector]
        public string transcibeFilePath { internal set; get; } = string.Empty;

        [HideInInspector]
        public string realStartDate { internal set; get; } = string.Empty;
        [HideInInspector]
        public string realStartTime { internal set; get; } = string.Empty;
        [HideInInspector]
        public Vector3 actualPosition { internal set; get; } = Vector3.zero;
        [HideInInspector]
        public float lengthOfClip { internal set; get; }
        [HideInInspector]
        public int framesCount { internal set; get; }
        /// <summary>
        /// Start time of recording since the first frame
        /// </summary>
        [HideInInspector]
        public float frameTime { internal set; get; }

        [SerializeField]
        private TMPro.TextMeshPro labelTxt;

        [SerializeField]
        private ProgressIndicatorLoadingBar loadingBar = default;

        [SerializeField]
        private Transform sphere;
        [SerializeField]
        private Transform logoFront = default;
        [SerializeField]
        private Transform logoBack = default;


        [SerializeField]
        private Material matIdle = default;
        [SerializeField]
        private Material matRecording = default;
        [SerializeField]
        private Material matPlaying = default;

        [SerializeField]
        private Material matMic = default;
        [SerializeField]
        private Material matPlayBtn = default;
        [SerializeField]
        private Material matStopBtn = default;
        [SerializeField]
        private Material matDefault = default;


        private MeshRenderer sphereMRenderer;
        private MeshRenderer logoBackMRenderer;
        private MeshRenderer logoFrontMRenderer;



        //private constant
        private const float rotationSpeed = 100f;
        private const float scaleSpeed = 4f;

        private float passedTime = 0;

        //Interaction fields
        private const float pressMinThreshold = 2f;
        private float touchTime;
        private int touchState; // 0 - untouched, 1- touched, 2- grabbed, 3- passed threshold



        #region PUBLIC_API
        //Interaction (View)
        public void TouchStart(HandTrackingInputEventData data)
        {
            touchState = 1;
            touchTime = Time.fixedTime;
            EWManager.Confirm("Touch start at: " + touchTime);
        }

        public void TouchEnd(HandTrackingInputEventData data)
        {
            touchState = 0;
        }

        public void Grabbed(ManipulationEventData data)
        {
            touchState = 2;
        }
        //End of interaction (View)

        public void PressedByHand()
        {
            this.transform.parent.GetComponent<RecordingBubbleManager>().SelectRecordBubble(this.gameObject);
        }

        public void SetRealStartTime(DateTime time)
        {
            this.realStartTime = time.ToString("HH:mm:ss");
        }
        #endregion
        #region UNITY_METHODS
        private void Awake()
        {
            sphereMRenderer = sphere.GetComponent<MeshRenderer>();
            logoBackMRenderer = logoBack.GetComponent<MeshRenderer>();
            logoFrontMRenderer = logoFront.GetComponent<MeshRenderer>();
            touchState = 0;
            loadingBar.Progress = 0;
            loadingBar.gameObject.SetActive(false);
            lengthOfClip = 1;
        }

        private void Start()
        {
            last_state = -1;
        }

        private void Update()
        {
            if (touchState == 1 && Time.fixedTime - touchTime > pressMinThreshold)
            {
                touchState = 3;
                EWManager.Confirm("Passed threshold at: " + Time.fixedTime);
                ObjectPressed();
            }
            if (state != last_state) //Change Material
            {
                last_state = state;
                loadingBar.Progress = 0;
                passedTime = 0;
                Material[] mats = new Material[2];
                switch (state)
                {
                    case 0:
                        sphereMRenderer.sharedMaterial = matIdle;
                        mats[0] = matPlayBtn;
                        loadingBar.gameObject.SetActive(false);
                        if (loadingBar.State != ProgressIndicatorState.Closed ||
                            loadingBar.State != ProgressIndicatorState.Closing) loadingBar.CloseAsync();
                        loadingBar.transform.parent.localPosition = new Vector3(0, 0.8f, 0);
                        break;
                    case 1:
                        sphereMRenderer.sharedMaterial = matRecording;
                        mats[0] = matMic;
                        loadingBar.gameObject.SetActive(false);
                        if (loadingBar.State != ProgressIndicatorState.Closed ||
                            loadingBar.State != ProgressIndicatorState.Closing) loadingBar.CloseAsync();
                        loadingBar.transform.parent.localPosition = new Vector3(0, 0.8f, 0);
                        break;
                    case 2:
                        sphereMRenderer.sharedMaterial = matPlaying;
                        mats[0] = matStopBtn;
                        loadingBar.gameObject.SetActive(true);
                        if (loadingBar.State != ProgressIndicatorState.Open ||
                            loadingBar.State != ProgressIndicatorState.Opening) loadingBar.OpenAsync();
                        loadingBar.transform.parent.localPosition = new Vector3(0, 0.95f, 0);
                        break;
                }
                mats[1] = matDefault;
                logoBackMRenderer.sharedMaterials = mats;
                logoFrontMRenderer.sharedMaterials = mats;
            }
            switch (state) //Movement, Rotation
            {
                case 1:
                    RecordingState();
                    PlayingState();
                    break;
                case 2:
                    PlayingState();
                    break;
            }
        }
        #endregion

        #region FUNCTIONALITY
        private void ObjectPressed()
        {
            if (state == 0)
            {
                PressedByHand();
                this.transform.parent.GetComponent<RecordingBubbleManager>().PlayPressed();
            }
            else if (state == 2)
            {
                PressedByHand();
                this.transform.parent.GetComponent<RecordingBubbleManager>().StopPlaying();
            }
        }
        private void RecordingState()
        {
            float newScale;
            passedTime += Time.unscaledDeltaTime;
            newScale = 1 + Mathf.Sin(passedTime * scaleSpeed) * 0.1f;
            sphere.localScale = new Vector3(newScale, newScale, newScale);
        }

        private void PlayingState()
        {
            passedTime += Time.unscaledDeltaTime;
            float rotateAroundY = sphere.localRotation.eulerAngles.y;
            var q = sphere.localRotation;
            sphere.localRotation = Quaternion.Euler(q.x, rotateAroundY - rotationSpeed * Time.unscaledDeltaTime, q.z);
            if (state == 2 && lengthOfClip > 0)
            {
                loadingBar.Progress = passedTime / lengthOfClip;
            }
        }

        private void UpdateLabelTxt()
        {
            if (labelTxt != null)
            {
                if (string.IsNullOrWhiteSpace(this.realStartTime))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(audioFilePath))
                        {
                            this.realStartTime = audioFilePath.Split('-')[1].Replace(",", ":");
                        }
                        else
                        {
                            this.realStartTime = sceneLogFilePath.Split('-')[1].Replace(",", ":");
                        }
                        labelTxt.text = this.realStartTime;
                    }
                    catch (Exception)
                    {
                        labelTxt.text = "X";
                        EWManager.Warning("Couldn't find start recording time from filename");
                    }
                }
                else
                {
                    labelTxt.text = this.realStartTime;
                }
            }
        }
        public void RefreshUI()
        {
            if (string.IsNullOrWhiteSpace(transcibeFilePath))
            {
                sphere.localPosition = Vector3.zero;
                this.gameObject.GetComponentInChildren<Audio.SpeechTextDialogue>().gameObject.SetActive(false);
            }
            UpdateLabelTxt();
        }
        #endregion
    }
}