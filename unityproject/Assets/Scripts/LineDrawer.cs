using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;


namespace HoloAuopsy
{
    public class LineDrawer : MonoBehaviour
    {
        private TrailRenderer trailRenderer;
        [SerializeField]
        private Transform paintingBoom = default;
        [SerializeField]
        private Material lineMaterial = default;
        [SerializeField]
        private Transform target = default;

        private MixedRealityPose pose;
        private bool onDrawing;
        void Start()
        {
            trailRenderer = GetComponent<TrailRenderer>();
            trailRenderer.startWidth = 0.005f;
            trailRenderer.endWidth = 0.005f;
            paintingBoom.GetComponent<MeshFilter>().mesh = new Mesh();
            onDrawing = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (!onDrawing && Input.GetMouseButtonDown(0)) //Start drawing
            {
                //if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out pose))
                //{
                //if (target.isLive) transform.position = target.lastValidPosition;
                transform.position = target.position;
                trailRenderer.Clear();
                onDrawing = true;
                //}
            }
            else if (onDrawing && Input.GetMouseButtonUp(0)) //Released, end drawing
            {
                Mesh mesh = new Mesh();
                trailRenderer.BakeMesh(mesh);
                GameObject obj = new GameObject();
                obj.transform.SetParent(paintingBoom);
                obj.AddComponent<MeshFilter>();
                obj.AddComponent<MeshRenderer>();
                obj.GetComponent<MeshRenderer>().sharedMaterial = lineMaterial;
                obj.GetComponent<MeshFilter>().mesh = mesh;
                trailRenderer.Clear();
                //Combine meshes when get large or after a period of time
                if (paintingBoom.childCount > 10)
                {
                    CombineMeshes();
                }
                onDrawing = false;
            }
            else if (onDrawing && Input.GetMouseButton(0)) //is drawing
            {
                //if (target.isLive) transform.position = target.lastValidPosition;
                transform.position = target.position;
                //if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out pose))
                //{
                //    transform.position = pose.Position;
                //}
            }
        }

        void CombineMeshes()
        {
            int childCount = paintingBoom.childCount;
            CombineInstance[] combineInstance = new CombineInstance[paintingBoom.childCount + 1];
            Transform objTransform;
            for (int i = 0; i < childCount; i++)
            {
                objTransform = paintingBoom.GetChild(i);
                combineInstance[i].mesh = objTransform.GetComponent<MeshFilter>().mesh;
                combineInstance[i].transform = objTransform.localToWorldMatrix;
            }
            if (paintingBoom.GetComponent<MeshFilter>().mesh != null)
            {
                GameObject obj = new GameObject();
                obj.transform.SetParent(paintingBoom);
                obj.AddComponent<MeshFilter>();
                obj.GetComponent<MeshFilter>().mesh = paintingBoom.GetComponent<MeshFilter>().mesh;
                combineInstance[childCount].mesh = obj.GetComponent<MeshFilter>().mesh;
                combineInstance[childCount].transform = obj.transform.localToWorldMatrix;
            }
            paintingBoom.GetComponent<MeshFilter>().mesh = new Mesh();
            paintingBoom.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combineInstance);
            while (paintingBoom.childCount > 0)
            {
                objTransform = paintingBoom.GetChild(0);
                objTransform.SetParent(null);
                objTransform.gameObject.SetActive(false);
                Object.Destroy(objTransform.gameObject);
            }
        }
    }
}