using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.Utils
{
    public class MathFunctions
    {
        private readonly static Vector3 coordOffset = new Vector3(-0.5f, -0.5f, -0.5f);
        private readonly static Ray[] boxLines = new Ray[] {
            new Ray(Vector3.zero+coordOffset, Vector3.right),               //0
            new Ray(Vector3.zero+coordOffset,Vector3.up),       //1
            new Ray(Vector3.zero+coordOffset,Vector3.forward),  //2
            new Ray(Vector3.right+coordOffset,Vector3.up),      //3
            new Ray(Vector3.right+coordOffset,Vector3.forward), //4
            new Ray(Vector3.up+coordOffset,Vector3.right),      //5
            new Ray(Vector3.up+coordOffset,Vector3.forward),    //6
            new Ray(Vector3.forward+coordOffset,Vector3.right), //7
            new Ray(Vector3.forward+coordOffset,Vector3.up),    //8
            new Ray(Vector3.one+coordOffset,Vector3.left),      //9
            new Ray(Vector3.one+coordOffset,Vector3.back),      //10
            new Ray(Vector3.one+coordOffset,Vector3.down)};     //11

        /// <summary>
        /// Returns intersected points of plane and box
        /// </summary>
        /// <param name="rayOrigin"></param>
        /// <param name="rayDir"></param>
        /// <param name="plane"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Vector3> IntersectPlaneScaleBox(Transform box, Transform plane)
        {
            List<Vector3> pointsList = new List<Vector3>();

            Vector3 normalVector = Vector3.Scale(box.localScale, Quaternion.Inverse(box.rotation) * plane.forward).normalized;
            Vector3 localPoint = box.InverseTransformPoint(plane.position);
            Plane localPlane = new Plane(normalVector, localPoint);

            for (int i = 0; i < 12; i++) // 12 lines
            {
                if (localPlane.Raycast(boxLines[i], out float length) && length <= 1f)
                {
                    Vector3 intersectedPoint = boxLines[i].origin + boxLines[i].direction * length;
                    pointsList.Add(box.localToWorldMatrix.MultiplyPoint(intersectedPoint));
                }
            }

            return pointsList;
        }
        public static Vector3 PolygonCenterPoint(List<Vector3> points)
        {
            
            return Vector3.zero;
        }
        /// <summary>
        /// Finds closest intersected point on the axes with the plane in the box local space
        /// </summary>
        /// <param name="box"></param>
        /// <param name="plane"></param>
        /// <returns>intersected point in world space</returns>
        public static Vector3 IntersectPlaneMainAxes(Transform box, Transform plane)
        {
            float length;
            float minLength = float.MaxValue;
            int axisNum = -1;
            List<Vector3> pointsList = new List<Vector3>();

            Vector3 normalVector = Vector3.Scale(box.localScale, Quaternion.Inverse(box.rotation) * plane.forward).normalized;
            Vector3 localPoint = box.InverseTransformPoint(plane.position);
            Plane localPlane = new Plane(normalVector, localPoint);

            if (Vector3.Magnitude(localPoint) < 0.01)
            {
                return box.localToWorldMatrix.MultiplyPoint(Vector3.zero);
            }

            //x axis
            if (localPlane.Raycast(new Ray(Vector3.zero, Vector3.right), out length) || length < 0)
            {
                minLength = length;
                axisNum = 0;
                pointsList.Add(new Vector3(length, 0, 0));
            }

            //y axis
            if (localPlane.Raycast(new Ray(Vector3.zero, Vector3.up), out length) || length < 0)
            {
                if (Mathf.Abs(length) < Mathf.Abs(minLength))
                {
                    minLength = length;
                    axisNum = 1;
                }
                pointsList.Add(new Vector3(0, length, 0));
            }

            //z axis
            if (localPlane.Raycast(new Ray(Vector3.zero, Vector3.forward), out length) || length < 0)
            {
                if (Mathf.Abs(length) < Mathf.Abs(minLength))
                {
                    minLength = length;
                    axisNum = 2;
                }
                pointsList.Add(new Vector3(0, 0, length));
            }

            //switch (axisNum)
            //{
            //    case 0:
            //        Debug.Log("0:" + minLength);
            //        return box.localtoworldmatrix.multiplypoint(new vector3(minlength, 0, 0));
            //    case 1:
            //        Debug.log("1:" + minlength);
            //        return box.localtoworldmatrix.multiplypoint(new vector3(0, minlength, 0));
            //    case 2:
            //        Debug.log("2:" + minlength);
            //        return box.localtoWorldMatrix.MultiplyPoint(new Vector3(0, 0, minLength));
            //}

            if (pointsList.Count == 1)
            {
                return box.localToWorldMatrix.MultiplyPoint(pointsList[0]);
            }
            if (pointsList.Count == 2)
            {

                return box.localToWorldMatrix.MultiplyPoint(pointsList[0]);
            }
            if (pointsList.Count == 3)
            {

                return box.localToWorldMatrix.MultiplyPoint(pointsList[0]);
            }

            Debug.Log("Wrong code! This algorithm shouldn't reach this statement.");
            return Vector3.zero;
        }

        public static Vector3[] LongestDiagonal(List<Vector3> points)
        {
            if (points.Count < 2) return null;

            Vector3[] vertexs = { Vector3.zero, Vector3.zero };
            int count = points.Count;
            float longestDiag = 0;

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    float length = Vector3.Distance(points[i], points[j]);
                    if (longestDiag < length)
                    {
                        longestDiag = length;
                        vertexs[0] = points[i];
                        vertexs[1] = points[j];
                    }
                }
            }
            return vertexs;
        }
    }

    //private static string LineName(int i)
    //{
    //    switch (i)
    //    {
    //        case 0:
    //            return "zero->right";
    //        case 1:
    //            return "zero->up";
    //        case 2:
    //            return "zero->forward";
    //        case 3:
    //            return "right->up";
    //        case 4:
    //            return "right->forward";
    //        case 5:
    //            return "up->right";
    //        case 6:
    //            return "up->forward";
    //        case 7:
    //            return "forward->right";
    //        case 8:
    //            return "forward->up";
    //        case 9:
    //            return "one->left";
    //        case 10:
    //            return "one->back";
    //        case 11:
    //            return "one->down";

    //    }
    //    return "Err";
    //}
}