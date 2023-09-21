using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.Utils
{
    public class StringUtils
    {
        public static string Vec3ToString(Vector3 vec)
        {
            if (vec == null)
            {
                return string.Empty;
            }
            return vec.x + "," + vec.y + "," + vec.z;
        }

        public static string QuaternionToString(Quaternion input)
        {
            if (input == null)
            {
                return string.Empty;
            }
            return input.x.ToString() + "," + input.y.ToString() + "," + input.z.ToString() + "," + input.w.ToString();
        }
        public static string TransformToString(Transform input)
        {
            if (input == null) return string.Empty;
            return Vec3ToString(input.localPosition) + "," + QuaternionToString(input.localRotation) + "," + Vec3ToString(input.localScale);
        }
        public static Tuple<Vector3,Quaternion,Vector3> StringToTransform(string str)
        {
            str.Split(',');

            return null;
        }
    }
}