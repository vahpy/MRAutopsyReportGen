using UnityEngine;

namespace HoloAutopsy.CuttingShape
{
    public class ShapeManipulatorTool : MonoBehaviour
    {
        [SerializeField]
        private MeshManipulator targetShapeObj = default;


        private Vector3 _last_Postition;
        private void Start()
        {
            _last_Postition = transform.position;
        }

        private void Update()
        {
            //need optimisation, just check the occlusion when the bounding boxes are occluded.

            //
            if (_last_Postition != transform.position)
            {
                Vector3 moveVec = _last_Postition - transform.position;
                _last_Postition = transform.position;
                float mag = Vector3.Dot(transform.forward, moveVec);


                var listPos = targetShapeObj.vertices;
                int count = 0;
                foreach (GameObject v in listPos)
                {
                    Vector3 localPos = this.transform.worldToLocalMatrix.MultiplyPoint(v.transform.position);
                    if (IsOccluded(localPos))
                    {
                        v.transform.position = this.transform.localToWorldMatrix.MultiplyPoint(MoveToBoundries(localPos));
                    }
                }
            }
        }

        private Vector3 MoveToBoundries(Vector3 localPos)
        {
            float minDist = Mathf.Abs(localPos.x - 0.5f);
            Vector3 newLocalPos = new Vector3(0.5f, localPos.y, localPos.z);

            if (minDist > Mathf.Abs(localPos.x + 0.5f))
            {
                newLocalPos = new Vector3(-0.5f, localPos.y, localPos.z);
            }
            if (minDist > Mathf.Abs(localPos.y - 0.5f))
            {
                newLocalPos = new Vector3(localPos.x, 0.5f, localPos.z);
            }
            if (minDist > Mathf.Abs(localPos.y + 0.5f))
            {
                newLocalPos = new Vector3(localPos.x, -0.5f, localPos.z);
            }
            if (minDist > Mathf.Abs(localPos.z - 0.5f))
            {
                newLocalPos = new Vector3(localPos.x, localPos.y, 0.5f);
            }
            if (minDist > Mathf.Abs(localPos.z + 0.5f))
            {
                newLocalPos = new Vector3(localPos.x, localPos.y, -0.5f);
            }

            return newLocalPos;
        }

        private bool IsOccluded(Vector3 localPoint)
        {
            if (localPoint.x < -0.5f || localPoint.x > 0.5f || localPoint.y < -0.5f || localPoint.y > 0.5f || localPoint.z < -0.5f || localPoint.z > 0.5f)
            {
                return false;
            }
            return true;
        }
    }
}