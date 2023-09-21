using HoloAutopsy;
using HoloAutopsy.Record;
using HoloAutopsy.Record.Audio;
using HoloAutopsy.Record.Logging;
using HoloAutopsy.Utils;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.Toolkit.UI;
using openDicom.DataStructure;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private VoiceRecorder voiceRecorder = default;
    [SerializeField] private GameObject playerPrefab = default;
    [SerializeField] private float defaultPlayerDistance = 0.4f;
    [SerializeField] private float leadInTime = 15f;
    [SerializeField] private int debugFrameNum = 0;
    [SerializeField] private string debugFilePath = string.Empty;
    [SerializeField] private bool debugEnabled = false;
    private bool _lastDebugEnabled;


    private ButtonConfigHelper _playBtnHelper;

    private IProgressIndicator progressBar;
    private TextMeshPro textProgressBar;
    private GameObject player = null;

    private int targetFrameNum = 0;
    //private float targetFrameTime = 0;

    private bool isPlaying = false;
    private bool _lastIsPlaying;

    

    private void Awake()
    {
        isPlaying = false;
        _lastDebugEnabled = debugEnabled;
        _lastIsPlaying = !isPlaying;
    }
    /**
     * It instantiates a "player" object from a "playerPrefab" and sets its position relative to the main camera.
     * */
    private void InitializeView()
    {
        player = Instantiate(playerPrefab, this.transform);
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        player.transform.position = cameraPosition + (cameraForward * defaultPlayerDistance);

        player.name = "Event Player";
        _playBtnHelper = player.transform.FindDeepChild("PlayPauseBtn").GetComponent<ButtonConfigHelper>();
        var backBtnHelper = player.transform.FindDeepChild("BackBtn").GetComponent<ButtonConfigHelper>();
        var nextBtnHelper = player.transform.FindDeepChild("NextBtn").GetComponent<ButtonConfigHelper>();
        var closeBtnHelper = player.transform.FindDeepChild("CloseBtn").GetComponent<ButtonConfigHelper>();
        _playBtnHelper.OnClick.AddListener(PlayPauseAction);
        backBtnHelper.OnClick.AddListener(BackFiveSec);
        nextBtnHelper.OnClick.AddListener(ForwardFiveSec);
        closeBtnHelper.OnClick.AddListener(CloseBtnAction);

        progressBar = player.GetComponentInChildren<IProgressIndicator>();

        textProgressBar = progressBar?.MainTransform.Find("CounterText")?.GetComponent<TextMeshPro>();
    }

    public void PlaybackEvent(int frameNum, string filePath)
    {
        if (this.transform.childCount == 0) InitializeView();

        if (player == null)
        {
            EWManager.Error("Player Controller must have only one gameobject, which is instantiated player object.");
            return;
        }
        player.SetActive(true);
        //Setup/Update Logging Manager File Reader
        LoggingManager.Instance.StopPlaying();
        RecordFileMetaInfo metaInfo = RecordedFileManager.GetMetaInfo(filePath);
        LoggingManager.Instance.SetRecordedFile(metaInfo);
        //

        this.targetFrameNum = frameNum;
        if (_playBtnHelper != null) _playBtnHelper.MainLabelText = "Play";
        else EWManager.Error("play button pointer is null");
        if (progressBar != null)
        {
            if (progressBar.State != ProgressIndicatorState.Open ||
                            progressBar.State != ProgressIndicatorState.Opening) progressBar.OpenAsync();
            progressBar.Progress = 0;
        }
        else EWManager.Error("progress bar pointer is null");
        LoggingManager.Instance.PlayFrames(0, this.targetFrameNum);
        UpdateProgressBar();
    }

    public void PlayPauseAction()
    {
        isPlaying = !isPlaying;
    }

    private void PlayBtnTextControl()
    {
        if (isPlaying)
        {
            if (_playBtnHelper != null) _playBtnHelper.MainLabelText = "Pause";
        }
        else
        {
            if (_playBtnHelper != null) _playBtnHelper.MainLabelText = "Play";
        }
    }

    public void BackFiveSec()
    {
        LoggingManager.Instance.ShiftFrameTime(-5);
        voiceRecorder.StopPlaying();
        if (isPlaying) voiceRecorder.LoadPlay(LoggingManager.Instance.MetaInfo.audioFile, LoggingManager.Instance.frameTime);
        
        UpdateProgressBar();
    }

    public void ForwardFiveSec()
    {
        LoggingManager.Instance.ShiftFrameTime(5);
        voiceRecorder.StopPlaying();
        if (isPlaying) voiceRecorder.LoadPlay(LoggingManager.Instance.MetaInfo.audioFile, LoggingManager.Instance.frameTime);
        
        UpdateProgressBar();
    }

    public void CloseBtnAction()
    {
        isPlaying = false;
        if(player!=null)
        {
            Destroy(player);
            player = null;
        }
    }
    // update progress bar
    private void Update()
    {
        // Debug function call
        if (_lastDebugEnabled != debugEnabled)
        {
            _lastDebugEnabled = debugEnabled;
            if (debugEnabled)
            {
                DebugAction();
            }
        }

        // end debug
        if (isPlaying != _lastIsPlaying)
        {
            _lastIsPlaying = isPlaying;

            if (isPlaying)
            {
                if (LoggingManager.Instance.IsPaused)
                {
                    LoggingManager.Instance.Resume();
                    voiceRecorder.LoadPlay(LoggingManager.Instance.MetaInfo.audioFile, LoggingManager.Instance.frameTime);
                }
                else
                {
                    LoggingManager.Instance.PlayFrames(leadInTime, targetFrameNum);
                    voiceRecorder.LoadPlay(LoggingManager.Instance.MetaInfo.audioFile, LoggingManager.Instance.frameTime);
                    //targetFrameTime = LoggingManager.Instance.frameTime + leadInTime;
                }
            }
            else
            {
                LoggingManager.Instance.Pause();
                voiceRecorder.StopPlaying();
            }
            PlayBtnTextControl();
        }
        else if (isPlaying && LoggingManager.Instance.IsPaused) // finished to the target frame
        {
            isPlaying = false;
        }
        if (isPlaying && LoggingManager.Instance.IsPlaying)
        {
            UpdateProgressBar();
        }

    }
    private void UpdateProgressBar()
    {
        progressBar.Progress = LoggingManager.Instance.frameTime / LoggingManager.Instance.MetaInfo.lengthOfClip;
        if (textProgressBar != null) { textProgressBar.text = LoggingManager.Instance.frameTime.ToString("N0")+" s"; }
        // following if for debugging
        if (LoggingManager.Instance.frameNum / LoggingManager.Instance.MetaInfo.framesCount > 1.0f) 
            EWManager.Log("len: " + LoggingManager.Instance.MetaInfo.lengthOfClip + ", frametime: " + LoggingManager.Instance.frameTime + ", frac: " +
                (LoggingManager.Instance.frameTime / LoggingManager.Instance.MetaInfo.lengthOfClip));
    }
    // Debug action in simulator
    public void DebugAction()
    {
        PlaybackEvent(debugFrameNum, debugFilePath);
    }
}
