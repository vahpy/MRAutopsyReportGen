using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityVolumeRendering;
namespace HoloAutopsy.CuttingShape
{
    [ExecuteInEditMode]
    public class MeshManipulator : MonoBehaviour
    {
        [SerializeField]
        private VolumeRenderedObject volRenObj;
        [SerializeField]
        private GameObject meshCubePrefab = default;
        [SerializeField]
        private Material selectMat = default;
        [SerializeField]
        private Material normalMat = default;
        [SerializeField]
        private float selectionRadius = 0.5f;


        [SerializeField]
        private Transform posLocator;

        private int currentSelectedVertex;

        [SerializeField]
        public Vector3[] verticesPos { private set; get; }
        [SerializeField]
        public List<GameObject> vertices { private set; get; }
        private List<int>[] neighborsMap;
        private List<int> neighborsOfSelectedVertex;

        [SerializeField]
        private UnityEvent meshUpdateEvent;

        private Vector3 defaultVertexPrefabScale;
        private Vector3 maxVertexPrefabScale;


        public bool IsChangingVerticesState;// { private set; get; }
        private bool changedVertex = false;
        void OnEnable()
        {
            var children = this.GetComponentsInChildren<Transform>();
            if (Application.isPlaying || children == null || children.Length <= 1)
            {
                foreach (Transform child in children)
                {
                    if (child != this.transform)
                    {
                        child.SetParent(null);
                        Destroy(child.gameObject);
                    }
                }
                vertices = null;

                maxVertexPrefabScale = new Vector3(0.05f, 0.05f, 0.05f);
                defaultVertexPrefabScale = new Vector3(0.005f, 0.005f, 0.005f);
                verticesPos = GetComponent<MeshFilter>().sharedMesh.vertices;
                vertices = new List<GameObject>(verticesPos.Length);

                for (int i = 0; i < verticesPos.Length; i++)
                {
                    var obj = Instantiate(meshCubePrefab, this.transform);
                    obj.transform.localPosition = verticesPos[i];
                    obj.transform.localScale = defaultVertexPrefabScale;
                    vertices.Add(obj);
                    obj.name = "vcube" + vertices.Count;
                }
            }
            else
            {
                if (vertices != null)
                {
                    vertices.RemoveAll(x => x != null);
                }
                else
                {
                    vertices = new List<GameObject>();
                }
                foreach (Transform child in children)
                {
                    if (child != this.transform)
                    {
                        vertices.Add(child.gameObject);
                    }
                }
                verticesPos = GetComponent<MeshFilter>().sharedMesh.vertices;
            }

            currentSelectedVertex = -1;
            CalcNeighbors();
            SetVisible(false);
        }

        void Update()
        {
            if (!volRenObj.GetCutShapeEnabled())
            {
                IsChangingVerticesState = false;
                SetVisible(false);
                return;
            }
            IsChangingVerticesState = true;
            SetVisible(true);

            // Update mesh if any vertex cube has moved
            for (int vertIdx = 0; vertIdx < vertices.Count; vertIdx++)
            {
                var vertex = vertices[vertIdx].transform;
                if (vertex.hasChanged)
                {
                    vertex.hasChanged = false;
                    if (verticesPos[vertIdx] != vertex.localPosition)
                    {
                        changedVertex = true;
                        if (currentSelectedVertex != vertIdx)
                        {
                            DeSelectAll(neighborsOfSelectedVertex);
                            currentSelectedVertex = vertIdx;
                            neighborsOfSelectedVertex = AllNeighborsInRadius(vertIdx, selectionRadius);
                            foreach (int j in neighborsOfSelectedVertex)
                            {
                                Select(vertices[j]);
                            }
                        }
                        MoveVerticesTypeTwo(vertIdx, neighborsOfSelectedVertex, verticesPos[vertIdx], vertex.localPosition);
                    }
                }
            }
            if (changedVertex)
            {
                GetComponent<MeshFilter>().sharedMesh.vertices = verticesPos;
                if (meshUpdateEvent != null) meshUpdateEvent.Invoke();
                changedVertex = false;
            }
            ScaleVerticesRelativeToCloseness(GetCurrentHandPosition());
        }

        public Vector3[] GetVerticesPos()
        {
            return verticesPos;
        }
        //fix nulls
        public int[] GetTriangles()
        {
            return this.GetComponent<MeshFilter>().sharedMesh.triangles;
        }
        public void AddVertexAction()
        {
            if (posLocator != null) AddVertex(posLocator.position);
        }

        public bool AddVertex(Vector3 position)
        {
            if (!IsChangingVerticesState) return false;
            int faceNum;
            var projPos = MeshUtils.GetProjectedPointPosition(this.transform, position, out faceNum);
            Debug.Log("Face Num:" + faceNum);
            if (faceNum >= 0)
            {
                int numVertices = this.transform.GetComponent<MeshFilter>().sharedMesh.vertices.Length;
                //Debug.Log("Vertices first:" + numVertices);
                MeshUtils.AddVertexToMesh(projPos, faceNum, this.transform);
                verticesPos = this.transform.GetComponent<MeshFilter>().sharedMesh.vertices;
                //Debug.Log("Vertices second:" + verticesPos.Length);
                if (verticesPos.Length > numVertices)
                {
                    var obj = Instantiate(meshCubePrefab, this.transform);
                    obj.transform.position = this.transform.localToWorldMatrix.MultiplyPoint(verticesPos[verticesPos.Length - 1]);
                    obj.transform.localScale = defaultVertexPrefabScale;
                    vertices.Add(obj);
                    obj.name = "vcube" + vertices.Count;
                    currentSelectedVertex = -1;
                    CalcNeighbors();
                }
                changedVertex = true;
                return true;
            }
            else
            {
                return false;
            }
        }


        private void ScaleVerticesRelativeToCloseness(Vector3 point)
        {
            if (Vector3.Distance(point, this.transform.position) > 2) return;
            //if (Vector3.Distance(point, this.transform.position) > 1) return; // Threshold for checking
            foreach (var vertex in vertices)
            {
                var dist = Vector3.Distance(vertex.transform.position, point);
                if (dist > 0.5) vertex.transform.localScale = defaultVertexPrefabScale;
                else
                {
                    vertex.transform.localScale = Vector3.Lerp(maxVertexPrefabScale, defaultVertexPrefabScale, -Mathf.Pow(2 * dist - 1, 2) + 1);
                }
                vertex.transform.hasChanged = false;
            }
        }

        private void MoveSimplestMethod(int vertId, Vector3 newPos)
        {
            if (vertId >= verticesPos.Length) return;
            verticesPos[vertId] = newPos;
        }
        private void MoveVerticesTypeTwo(int root, List<int> nearVerts, Vector3 oldPos, Vector3 newPos)
        {
            verticesPos[root] = newPos;
            Vector3 moveVector = newPos - oldPos;
            foreach (int vertIdx in nearVerts)
            {
                if (vertIdx == root) continue;
                if (vertIdx >= vertices.Count) break;
                var nearVertPos = verticesPos[vertIdx];
                var distance = Vector3.Distance(oldPos, nearVertPos);

                verticesPos[vertIdx] = nearVertPos + moveVector * Mathf.Lerp(1, 0.1f, distance / selectionRadius);
                vertices[vertIdx].transform.localPosition = verticesPos[vertIdx];
                vertices[vertIdx].transform.hasChanged = false;
            }
        }
        private void MoveVerticesTypeOne(int root, List<int> nearVerts, Vector3 oldPos, Vector3 newPos)
        {
            verticesPos[root] = newPos;
            Vector3 moveVector = newPos - oldPos;
            foreach (int vertIdx in nearVerts)
            {
                if (vertIdx == root) continue;
                if (vertIdx >= vertices.Count) break;
                var nearVertPos = verticesPos[vertIdx];
                var distance = Vector3.Distance(oldPos, nearVertPos);

                verticesPos[vertIdx] = nearVertPos + moveVector * Mathf.Lerp(1, 0, distance / selectionRadius);
                vertices[vertIdx].transform.localPosition = verticesPos[vertIdx];
                vertices[vertIdx].transform.hasChanged = false;
            }
        }
        public void DeSelectAll(List<int> listOfVetices)
        {
            if (listOfVetices == null) return;
            foreach (int vertId in listOfVetices)
            {
                if (vertId < vertices.Count) Deselect(vertices[vertId]);
            }
        }
        public void Select(GameObject selectedObj)
        {
            if (selectedObj != null)
            {
                selectedObj.GetComponent<Renderer>().sharedMaterial = selectMat;
            }
        }
        public void Deselect(GameObject deselectedObj)
        {
            if (deselectedObj != null)
            {
                deselectedObj.GetComponent<Renderer>().sharedMaterial = normalMat;
            }
        }

        private void CalcNeighbors()
        {
            neighborsMap = new List<int>[vertices.Count];
            for (int i = 0; i < neighborsMap.Length; i++)
            {
                neighborsMap[i] = new List<int>();
            }
            var triList = GetComponent<MeshFilter>().sharedMesh.triangles;
            for (int i = 0; i < triList.Length; i += 3)
            {
                AddUniqueNeighbor(triList[i], triList[i + 1]);
                AddUniqueNeighbor(triList[i], triList[i + 2]);
                AddUniqueNeighbor(triList[i + 1], triList[i]);
                AddUniqueNeighbor(triList[i + 1], triList[i + 2]);
                AddUniqueNeighbor(triList[i + 2], triList[i]);
                AddUniqueNeighbor(triList[i + 2], triList[i + 1]);
            }
            for (int i = 0; i < vertices.Count; i++)
            {
                for (int j = 0; j < vertices.Count; j++)
                {
                    if (Vector3.Distance(verticesPos[i], verticesPos[j]) < 0.0001)
                    {
                        AddUniqueNeighbor(i, j);
                        AddUniqueNeighbor(j, i);
                    }
                }
            }
        }
        private void AddUniqueNeighbor(int main, int neigh)
        {
            if (neighborsMap[main].Contains(neigh)) return;
            neighborsMap[main].Add(neigh);
        }

        /// <summary>
        ///Returns a list of all vertices that are in a radius, as well as the path of the root to that vertex should be in the list
        /// </summary>
        /// <param name="vertIdx"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private List<int> AllNeighborsInRadius(int vertIdx, float radius)
        {
            int pointer = 0;
            List<int> nearVertices = new List<int>();
            nearVertices.Add(vertIdx);
            
            while (pointer < nearVertices.Count)
            {
                var pointerNeighbors = neighborsMap[nearVertices[pointer]];

                foreach (int idx in pointerNeighbors)
                {
                    if (!nearVertices.Contains(idx))
                    {
                        if (Vector3.Distance(verticesPos[vertIdx], verticesPos[idx]) < radius)
                        {
                            nearVertices.Add(idx);
                        }
                    }
                }

                pointer++;
            }

            return nearVertices;
        }
        public void MoveVertex(int vertexId, Vector3 pos)
        {
            if (verticesPos[vertexId] != pos)
            {
                verticesPos[vertexId] = pos;
                if (vertices[vertexId].transform.localPosition != pos)
                {
                    vertices[vertexId].transform.localPosition = pos;
                }
            }
        }
        private Vector3 GetCurrentHandPosition()
        {
            MixedRealityPose pose;
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out pose))
            {
                return pose.Position;
            }
            return Vector3.positiveInfinity;
        }

        public Bounds GetMeshBounds()
        {
            return GetComponent<MeshFilter>().sharedMesh.bounds;
        }

        public void SetVisible(bool enable)
        {
            //this.gameObject.SetActive(enable);
            if (this.gameObject.GetComponent<MeshRenderer>().enabled == enable) return;
            this.gameObject.GetComponent<MeshRenderer>().enabled = enable;
            var childRenderers = this.GetComponentsInChildren<MeshRenderer>();
            if (childRenderers != null)
            {
                foreach (var child in childRenderers)
                {
                    child.enabled = enable;
                }
            }
        }
        public void ToggleCuttingBoxVisible()
        {
            SetVisible(!this.gameObject.GetComponent<MeshRenderer>().enabled);
        }
        public void ResetBoxMesh()
        {
            this.gameObject.GetComponent<MeshFilter>().sharedMesh = null;
            var listChildren = this.gameObject.transform.GetComponentsInChildren<Transform>();
            foreach (Transform child in listChildren)
            {
                GameObject.Destroy(child.gameObject);
            }
            this.gameObject.SetActive(false);
            this.gameObject.SetActive(true);

        }

        private Vector3 GetVertexLocalPos(int id)
        {
            return vertices[id].transform.localPosition;
        }
    }
}