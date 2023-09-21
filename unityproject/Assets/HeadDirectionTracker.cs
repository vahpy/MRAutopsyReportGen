using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HeadDirectionTracker : MonoBehaviour
{
    [SerializeField] private Transform lookDirectionTarget;
    [SerializeField] private Transform model;
    [SerializeField] private float modelRotationSpeed = 40;
    public bool trackHeadBody;
    private Transform mainCamera;
    private Vector3 lastLookDirLocPos;
    //    private Vector3 defaultForwardVector;
    Coroutine smoothRotationCoroutine;
    bool runModelRotate = false;
    void Start()
    {
        mainCamera = Camera.main.transform;
        trackHeadBody = true;
        lastLookDirLocPos = Vector3.zero;
        //        defaultForwardVector = model.forward;
    }
    // Update is called once per frame
    void Update()
    {
        if (trackHeadBody)
        {
            lookDirectionTarget.transform.position = mainCamera.forward + mainCamera.position;
            lookDirectionTarget.transform.rotation = mainCamera.rotation;
            model.position = mainCamera.position;
        }
        //Vector3 targetDirection = (new Vector3(lookDirectionTarget.position.x, model.position.y, lookDirectionTarget.position.z) - model.position).normalized;
        Vector3 targetDirection = (lookDirectionTarget.position - model.position).normalized;
        var targetRotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
        //var angle = Quaternion.Angle(model.rotation, targetRotation);
        //if (angle < 0.1)
        //{
        //    runModelRotate = false;
        //}
        //if (runModelRotate || angle > 25)
        //{
        //    model.rotation = Quaternion.RotateTowards(model.rotation, targetRotation, Time.deltaTime * modelRotationSpeed);
        //    runModelRotate = true;
        //}
        if (lastLookDirLocPos != lookDirectionTarget.transform.localPosition)
        {
            //print("lookDirectionTarget.position" + lookDirectionTarget.position + ", model.position: " + model.position);
            lastLookDirLocPos = lookDirectionTarget.transform.localPosition;
            if (smoothRotationCoroutine != null) StopCoroutine(smoothRotationCoroutine);
            smoothRotationCoroutine = StartCoroutine(smoothBodyRotation(targetRotation));
        }
    }

    private IEnumerator smoothBodyRotation(Quaternion targetRotation)
    {
        var angle = Quaternion.Angle(model.rotation, targetRotation);
        while (angle > 0.1)
        {
            model.rotation = Quaternion.RotateTowards(model.rotation, targetRotation, Time.deltaTime * modelRotationSpeed);
            angle = Quaternion.Angle(model.rotation, targetRotation);
            yield return null;
        }
    }
}
