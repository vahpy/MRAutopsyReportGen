using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.CuttingShape
{
    public static class MeshUtils
    {
        #region PUBLIC_API  

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pointOnFace">world pos</param>
        /// <param name=""></param>
        /// <returns></returns>
        public static bool AddVertexToMesh(Vector3 pointOnFace, int faceNum, Transform obj)
        {
            var tris = obj.GetComponent<MeshFilter>().sharedMesh.triangles;
            var verts = obj.GetComponent<MeshFilter>().sharedMesh.vertices;
            var v1 = verts[tris[faceNum * 3]];
            var v2 = verts[tris[faceNum * 3 + 1]];
            var v3 = verts[tris[faceNum * 3 + 2]];

            var locPoint = obj.worldToLocalMatrix.MultiplyPoint(pointOnFace);

            //nearest edge
            Vector3 e1, e2;
            NearestEdge(locPoint, out e1, out e2, v1, v2, v3);

            //Find the selected face (if there is one)
            int neighborFace = NearestNeighborFace(faceNum, e1, e2, tris, verts);

            Debug.Log("FaceNums: " + faceNum + ", " + neighborFace);
            string str = "";
            int count = 0;
            foreach(var v in tris)
            {
                str += v + " ";
                count++;
                if(count%2 == 0)
                {
                    str += "| ";
                }
            }
            Debug.Log(str);
            if (neighborFace >= 0)
            {
                //Merge two faces
                MergeTwoFaces(locPoint, faceNum, neighborFace, e1, e2, ref tris, ref verts);
                obj.GetComponent<MeshFilter>().sharedMesh.vertices = verts;
                obj.GetComponent<MeshFilter>().sharedMesh.triangles = tris;
                obj.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
                obj.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
                obj.GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();

                str = "";
                count = 0;
                foreach (var v in tris)
                {
                    str += v + " ";
                    count++;
                    if (count % 2 == 0)
                    {
                        str += "| ";
                    }
                }
                Debug.Log(str);
                return true;
            }
            else
            {
                // Make #facenum face triangle to three triangles (not usual)

            }
            return false;
        }

        private static void MergeTwoFaces(Vector3 newVector, int face1, int face2, Vector3 e1, Vector3 e2, ref int[] triangles, ref Vector3[] vertices)
        {
            List<Vector3> newVertices = new List<Vector3>(vertices);
            List<int> newTriangles = new List<int>(triangles);

            newVertices.Add(newVector);

            int newVectorId = newVertices.Count - 1;

            int p1 = PivotNode(e1, e2, face1, triangles, vertices);
            int p2 = PivotNode(e1, e2, face2, triangles, vertices);

            Debug.Log(p1 + "," + p2);

            int offset = face1 * 3;
            switch (p1)
            {
                case 0:
                    // (0,1,p) , (2,0,p)
                    // triangle 1
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(newVectorId);

                    // triangle 2
                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(newVectorId);
                    break;
                case 1:
                    // (0,1,p) , (1,2,p)
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(newVectorId);

                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(newVectorId);
                    break;
                case 2:
                    // (1,2,p) , (2,0,p)
                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(newVectorId);

                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(newVectorId);
                    break;
            }
            offset = face2 * 3;
            switch (p2)
            {
                case 0:
                    // (0,1,p) , (2,0,p)
                    // triangle 1
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(newVectorId);

                    // triangle 2
                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(newVectorId);
                    break;
                case 1:
                    // (0,1,p) , (1,2,p)
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(newVectorId);

                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(newVectorId);
                    break;
                case 2:
                    // (1,2,p) , (2,0,p)
                    newTriangles.Add(triangles[offset + 1]);
                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(newVectorId);

                    newTriangles.Add(triangles[offset + 2]);
                    newTriangles.Add(triangles[offset + 0]);
                    newTriangles.Add(newVectorId);
                    break;
            }

            if (face1 > face2)
            {
                newTriangles.RemoveRange(face1 * 3, 3);
                newTriangles.RemoveRange(face2 * 3, 3);
            }
            else
            {
                newTriangles.RemoveRange(face2 * 3, 3);
                newTriangles.RemoveRange(face1 * 3, 3);
            }
            triangles = newTriangles.ToArray();
            vertices = newVertices.ToArray();

            /*************
             * debugging messages
             * */
            //Debug.Log("Tris1:");
            //string str = "";
            //int count = 0;
            //foreach (int i in triangles)
            //{
            //    str += i + ",";
            //    if (count % 3 == 2)
            //    {
            //        str += "| ";
            //    }
            //    count++;
            //}
            //Debug.Log(str);
            //Debug.Log("Tris2:");
            //str = "";
            //count = 0;
            //foreach (int i in newTriangles.ToArray())
            //{
            //    str += i + " ";
            //    if (count % 3 == 2)
            //    {
            //        str += "| ";
            //    }
            //    count++;
            //}
            //Debug.Log(str);
        }

        /// <summary>
        /// Returns the vertex id that is not included in the edge that will be removed
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <param name="faceNum"></param>
        /// <param name="tris"></param>
        /// <param name="verts"></param>
        /// <returns></returns>
        private static int PivotNode(Vector3 e1, Vector3 e2, int faceNum, int[] tris, Vector3[] verts)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!SameVertex(verts[tris[faceNum * 3 + i]], e1) && !SameVertex(verts[tris[faceNum * 3 + i]], e2))
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool RemoveVertexFromMesh()
        {
            return false;
        }

        public static Vector3[] GetNeighbors()
        {

            return new Vector3[0];
        }

        public static float DistancePointToLine(Vector3 point, Vector3 line1, Vector3 line2)
        {
            Vector3 line = line2 - line1;
            if (line.magnitude <= 0.00001) return 0;
            return Vector3.Cross(point - line1, line).magnitude / line.magnitude;
        }

        /// <summary>
        /// Selected Face has a face number >=0, otherwise facenum is a negative number and return value Vector3.zero
        /// </summary>
        public static Vector3 GetProjectedPointPosition(Transform obj, Vector3 realWorldPos, out int faceNum)
        {
            var tris = obj.GetComponent<MeshFilter>().sharedMesh.triangles;
            var vert = obj.GetComponent<MeshFilter>().sharedMesh.vertices;

            var locRealPos = obj.worldToLocalMatrix.MultiplyPoint(realWorldPos);

            List<Vector3> projectedPointsList = new List<Vector3>();
            List<int> facesList = new List<int>();

            // Find all projected points on valid faces (triangles)
            for (int i = 0; i < tris.Length; i += 3)
            {
                var p1 = vert[tris[i]];
                var p2 = vert[tris[i + 1]];
                var p3 = vert[tris[i + 2]];

                var locProjectedPoint = ProjectPointOnPlane(locRealPos, p1, p2, p3);

                if (IsInsideTriangle(locProjectedPoint, p1, p2, p3))
                {
                    projectedPointsList.Add(locProjectedPoint);
                    facesList.Add(i / 3);
                }
            }

            // Select the nearest projected point to the real pos
            var selectedFace = -1;
            var selectedProjectedPoint = Vector3.zero;

            var minDistance = float.MaxValue;

            for (int i = 0; i < projectedPointsList.Count; i++)
            {
                var dist = Vector3.Distance(projectedPointsList[i], locRealPos);
                if (dist < minDistance)
                {
                    selectedFace = facesList[i];
                    selectedProjectedPoint = projectedPointsList[i];
                    minDistance = dist;
                }
            }

            faceNum = selectedFace;
            if (faceNum >= 0)
                return obj.localToWorldMatrix.MultiplyPoint(selectedProjectedPoint);
            else return Vector3.zero;
        }

        #endregion

        #region HELPER_METHOD
        private static Vector3 ProjectPointOnPlane(Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var normal = Vector3.Normalize(Vector3.Cross(p3 - p1, p2 - p1));
            var vec = point - p1;
            var projectedVec = Vector3.Dot(vec, normal) * normal;
            var onPlanePoint = point - projectedVec;

            return onPlanePoint;
        }

        private static bool IsInsideTriangle(Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return
                PointInOrOn(point, p1, p2, p3) &
                PointInOrOn(point, p2, p3, p1) &
                PointInOrOn(point, p3, p1, p2);
        }
        private static bool PointInOrOn(Vector3 P1, Vector3 P2, Vector3 A, Vector3 B)
        {
            Vector3 CP1 = Vector3.Cross(B - A, P1 - A);
            Vector3 CP2 = Vector3.Cross(B - A, P2 - A);
            if (Vector3.Dot(CP1, CP2) >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Using distance for finding same vertex, because many meshes same different vertex for a point in a mesh
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private static bool SameVertex(Vector3 p1, Vector3 p2)
        {
            if (p1 == p2) return true;
            return Vector3.Distance(p1, p2) < 0.00001;
        }

        private static int NearestNeighborFace(int faceNum, Vector3 e1, Vector3 e2, int[] tris, Vector3[] verts)
        {
            for (int i = 0; i < tris.Length; i += 3)
            {
                int commonVertex = 0;
                if (SameVertex(e1, verts[tris[i]])) commonVertex++;
                if (SameVertex(e1, verts[tris[i + 1]])) commonVertex++;
                if (SameVertex(e1, verts[tris[i + 2]])) commonVertex++;
                if (SameVertex(e2, verts[tris[i]])) commonVertex++;
                if (SameVertex(e2, verts[tris[i + 1]])) commonVertex++;
                if (SameVertex(e2, verts[tris[i + 2]])) commonVertex++;

                if (commonVertex == 2 && i / 3 != faceNum)
                {
                    return i / 3;
                }
            }
            return -1;
        }

        private static void NearestEdge(Vector3 locPoint, out Vector3 p1, out Vector3 p2, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            p1 = v1;
            p2 = v2;
            float dist = DistancePointToLine(locPoint, v1, v2);
            float minDist = dist;

            dist = DistancePointToLine(locPoint, v2, v3);

            if (dist < minDist)
            {
                minDist = dist;
                p1 = v2;
                p2 = v3;
            }

            dist = DistancePointToLine(locPoint, v3, v1);
            if (dist < minDist)
            {
                p1 = v1;
                p2 = v3;
            }
        }
        #endregion
    }
}