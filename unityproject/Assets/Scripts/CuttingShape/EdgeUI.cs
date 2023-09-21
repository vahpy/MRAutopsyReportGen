using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.CuttingShape
{
    [ExecuteInEditMode]
    public class EdgeUI : MonoBehaviour
    {
        [SerializeField]
        private MeshManipulator meshedObject;
        List<GameObject> edges;

        private bool isLineVisible;
        void OnEnable()
        {
            if (meshedObject == null) return;
            this.transform.localScale = meshedObject.transform.localScale;
            this.transform.localPosition = meshedObject.transform.localPosition;
            this.transform.localRotation = meshedObject.transform.localRotation;

            var meshFilter = meshedObject.GetComponent<MeshFilter>();
            if (meshFilter == null) return;

            if (meshFilter.sharedMesh == null) return;
            isLineVisible = true;
            EdgeChanged();
        }

        void Update()
        {
            if (isLineVisible != meshedObject.IsChangingVerticesState)
            {
                isLineVisible = meshedObject.IsChangingVerticesState;
                SetLinesVisible(isLineVisible);
            }
        }

        private void SetLinesVisible(bool visible)
        {
            var linesRen = this.GetComponentsInChildren<LineRenderer>();
            foreach(var line in linesRen)
            {
                line.enabled = visible;
            }
        }

        public void EdgeChanged()
        {
            DrawEdges(meshedObject.GetVerticesPos(), meshedObject.GetTriangles(), 0);
        }

        private void DrawEdges(Vector3[] verts, int[] tris, int numTry)
        {
            if (verts == null || tris == null) return;

            int edgeCount = tris.Length;
            var edgeLines = this.GetComponentsInChildren<LineRenderer>();
            //Debug.Log(edgeLines.Length + " vs " + tris.Length);
            if (edgeLines.Length == edgeCount)
            {
                //move edges
                for (int i = 0; i < edgeCount; i+=3)
                {
                    var pos1 = meshedObject.transform.localToWorldMatrix.MultiplyPoint(verts[tris[i]]);
                    var pos2 = meshedObject.transform.localToWorldMatrix.MultiplyPoint(verts[tris[i+1]]);
                    var pos3 = meshedObject.transform.localToWorldMatrix.MultiplyPoint(verts[tris[i+2]]);

                    edgeLines[i].SetPositions(new Vector3[] { pos1, pos2 });
                    edgeLines[i+1].SetPositions(new Vector3[] { pos2, pos3 });
                    edgeLines[i+2].SetPositions(new Vector3[] { pos3, pos1 });
                }
            }
            else
            {
                //remove and create all edges again
                Debug.Log("remove and create all edes again");
                RemoveRecreateEdges(edgeCount);
                if (numTry == 0)
                {
                    DrawEdges(verts, tris, numTry + 1);
                }
                else
                {
                    Debug.Log("Bug in code!");
                }
            }
        }

        private void RemoveRecreateEdges(int edgeCount)
        {
            var children = this.GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child != this.transform)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    Destroy(child);
                }
            }
            for (int i = 0; i < edgeCount; i++)
            {
                GameObject line = new GameObject();
                line.SetActive(true);
                line.transform.parent = this.transform;
                line.transform.localPosition = Vector3.zero;
                line.AddComponent<LineRenderer>();
                var lineRen = line.GetComponent<LineRenderer>();
                lineRen.enabled = true;
                lineRen.positionCount = 2;
                lineRen.useWorldSpace = true;
                lineRen.startWidth = 0.002f;
                lineRen.endWidth = 0.002f;
                lineRen.startColor = Color.red;
                lineRen.endColor = Color.red;
            }
            isLineVisible = true;
        }
    }
}