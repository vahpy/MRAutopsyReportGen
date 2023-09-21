using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.Utils
{
    public static class TransformUtils
    {

        public static Vector3 TransformWorldPositionToLocalTargetSpace(Vector3 worldPos, Transform targetSpace)
        {
            return targetSpace.InverseTransformPoint(worldPos);
        }
        public static Vector3 TransformLocalPositionToWorldSpace(Vector3 locPos, Transform originSpace)
        {
            return originSpace.TransformPoint(locPos);
        }

        public static Quaternion TransformWorldRotationToLocalTargetSpace(Quaternion worldRot, Transform targetSpace)
        {   
            Quaternion localRot = Quaternion.Inverse(targetSpace.rotation) * worldRot;
            return localRot;
        }

        public static Quaternion TransformLocalRotationToWorldSpace(Quaternion localRot, Transform originSpace)
        {
            Quaternion worldRot = originSpace.rotation * localRot;
            return worldRot;
        }

        public static Transform FindDeepChild(this Transform parent, string name)
        {
            Transform result = parent.Find(name);
            if (result != null) return result;
            foreach (Transform child in parent)
            {
                result = child.FindDeepChild(name);
                if (result != null) return result;
            }
            return null;
        }
    }
}