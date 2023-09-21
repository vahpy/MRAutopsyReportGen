using UnityEngine;

namespace HoloAutopsy
{
    //[ExecuteInEditMode]
    public class TV : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 10f)]
        float tvDistanceToCamera = 1;
        Camera mainCamera = default;
        // Start is called before the first frame update
        void Awake()
        {
            mainCamera = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
            //Debug.Log(mainCamera.transform.forward.y);
            //Vector3 temp = new Vector3(transform.localRotation.x,-mainCamera.transform.forward.y, transform.localRotation.z);
            //transform.localRotation = Quaternion.Euler(temp);
            Vector3 temp = mainCamera.transform.forward * tvDistanceToCamera + mainCamera.transform.position;
            transform.position = new Vector3(temp.x,transform.position.y,temp.z);

            Vector2 cameraProjectedForw = new Vector2(mainCamera.transform.forward.x, mainCamera.transform.forward.z);
            Vector2 projectedForw = new Vector2(-transform.forward.x, -transform.forward.z);
            float angle = Vector2.SignedAngle(cameraProjectedForw, projectedForw);

            if (angle > 0.1f || angle < -0.1f)
            {
                transform.RotateAround(mainCamera.transform.position, Vector3.up, angle);
            }
        }
    }
}