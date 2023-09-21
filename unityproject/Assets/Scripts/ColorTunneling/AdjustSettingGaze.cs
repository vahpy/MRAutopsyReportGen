using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustSettingGaze : MonoBehaviour
{
    private IMixedRealityEyeGazeProvider gazeProvider = default;
    private float defaultDistanceInMeters = 3;
    private bool? prevCalibrationStatus = null;
    // Start is called before the first frame update
    void Start()
    {
        gazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
    }

    // Update is called once per frame
    void Update()
    {
        if (gazeProvider == null) return;
        IsEyeCalibrated();
        if (gazeProvider.HitInfo.raycastValid)
        {
            transform.localPosition = gazeProvider.HitPosition;
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, gazeProvider.HitNormal);
        }
        else
        {
            transform.localPosition = gazeProvider.GazeOrigin + gazeProvider.GazeDirection.normalized * defaultDistanceInMeters;
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, -gazeProvider.GazeDirection.normalized);
        }
    }
    private void IsEyeCalibrated()
    {
        // Get the latest calibration state from the EyeGazeProvider
        bool? calibrationStatus = gazeProvider.IsEyeCalibrationValid;

        if (calibrationStatus != null)
        {
            if (prevCalibrationStatus != calibrationStatus)
            {
                if (calibrationStatus == false)
                {
                    //OnNoEyeCalibrationDetected.Invoke();
                    Debug.Log("\nOnNoEyeCalibrationDetected.Invoke();");

                }
                else
                {
                    //OnEyeCalibrationDetected.Invoke();
                    Debug.Log("|");

                }

                prevCalibrationStatus = calibrationStatus;
            }
        }
    }
}
