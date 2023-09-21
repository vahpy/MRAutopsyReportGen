using UnityEngine;
namespace HoloAutopsy.CuttingShape
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshManipulator))]
    public class MeshToShaderUploader : MonoBehaviour
    {
        private const int MAX_MESH_TRIANGLES = 100; // SYNC WITH SHADER CODE
        [SerializeField]
        private Transform volumeObj = default;
        int[] tris;
        Vector3[] vertices;

        Vector3 shaderOffset = new Vector3(0.5f, 0.5f, 0.5f);
        private bool isFirstTime = true;
        private void Start()
        {
            isFirstTime = true;
            tris = GetComponent<MeshFilter>().sharedMesh.triangles;
            vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        }

        void Update()
        {
            if (!this.GetComponent<MeshManipulator>().IsChangingVerticesState) return;
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                if (tris.Length > 300)
                {
                    Debug.Log("Max 100 triangles are supported!");
                    return;
                }
                //Debug.Log("Number of triangles: " + tris.Length + ", number of vertices: " + vertices.Length);
                Vector4[] vec4Tris;
                // Graphics API doesn't accept larger array after first initialisation,
                // so needs to be initialised with the largest possible array at first set vector call
                //if (isFirstTime)
                //{
                    vec4Tris = new Vector4[MAX_MESH_TRIANGLES * 3 + 1];
                    isFirstTime = false;
                //}
                //else
                //{
                //    vec4Tris = new Vector4[tris.Length + 1];
                //}
                int i;
                for (i = 0; i < tris.Length; i += 3)
                {
                    vec4Tris[i] = ToVolumeLocalSpace(vertices[tris[i]]);
                    vec4Tris[i + 1] = ToVolumeLocalSpace(vertices[tris[i + 1]]);
                    vec4Tris[i + 2] = ToVolumeLocalSpace(vertices[tris[i + 2]]);
                }
                //for (i = 36; i < tris.Length; i += 3)
                //{
                //    Debug.Log("Tri #" + (i / 3) + ": " + tris[i] + " " + tris[i + 1] + " " + tris[i + 2]);
                //}
                vec4Tris[i] = new Vector4(-1000, -1000, -1000, -1000);
                volumeObj.GetComponent<MeshRenderer>().sharedMaterial.SetVectorArray("_MyTriangle", vec4Tris);
            }
        }

        public void TriggerMeshUpdate()
        {
            Debug.Log("Mesh update triggered");
            tris = GetComponent<MeshFilter>().sharedMesh.triangles;
            vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
            //Debug.Log("tri: "+tris.Length+", vertices: "+vertices.Length);
            transform.hasChanged = true;
        }

        private Vector4 ToVolumeLocalSpace(Vector3 pos)
        {
            Vector3 vec = transform.localToWorldMatrix.MultiplyPoint(pos);
            vec = volumeObj.worldToLocalMatrix.MultiplyPoint(vec);
            vec += shaderOffset;
            return new Vector4(vec.x, vec.y, vec.z);
        }
    }
}