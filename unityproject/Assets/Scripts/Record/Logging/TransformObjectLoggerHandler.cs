using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace HoloAutopsy.Record.Logging
{
    public class TransformObjectLoggerHandler : MonoBehaviour, ObjectLogger
    {
        Vector3 _lastPos;
        Quaternion _lastRot;
        Vector3 _lastScale;

        [SerializeField]
        private bool ignorePosReplay = false;

        private const string STR_TRANSFORM = "transform";

        // Undo Feature
        private Transform beforeCallFrameTransform;

        private void Start()
        {
            beforeCallFrameTransform = this.transform;
        }

        public string Fetch(int frameNum)
        {
            string str = string.Empty;
            if (frameNum == 0 || _lastPos != transform.localPosition || _lastRot != transform.localRotation || _lastScale != transform.localScale)
            {
                _lastPos = transform.localPosition;
                _lastRot = transform.localRotation;
                _lastScale = transform.localScale;

                str = STR_TRANSFORM + ",";
                str += Vector3ToString(transform.localPosition) + ",";
                str += QuaternionToString(transform.localRotation) + ",";
                str += Vector3ToString(transform.localScale) + "\n";
            }

            return str;
        }


        public void Call(string[] td)
        {
            //Debug.Log("Transform logger called on " + this.gameObject.name);
            if (td == null) return;
            if (td[1].Equals(STR_TRANSFORM))
            {
                beforeCallFrameTransform = transform;
                UpdateTransform(td[2], td[3], td[4], td[5], td[6], td[7], td[8], td[9], td[10], td[11]);
            }
        }

        public void ResetChangeTrackers()
        {
            _lastPos = Vector3.zero;
            _lastRot = Quaternion.identity;
            _lastScale = Vector3.zero;
        }
        public string GetName()
        {
            return gameObject.name;
        }

        private void UpdateTransform(string px, string py, string pz, string rx, string ry, string rz, string rw, string sx, string sy, string sz)
        {
            if (!ignorePosReplay) transform.localPosition = new Vector3(ParseFloat(px), ParseFloat(py), ParseFloat(pz));
            transform.localRotation = new Quaternion(ParseFloat(rx), ParseFloat(ry), ParseFloat(rz), ParseFloat(rw));
            transform.localScale = new Vector3(ParseFloat(sx), ParseFloat(sy), ParseFloat(sz));
            //Debug.Log("Transform updated on "+this.gameObject.name);
        }

        private static float ParseFloat(string floatStr)
        {
            return float.Parse(floatStr, CultureInfo.InvariantCulture.NumberFormat);
        }

        private static string Vector3ToString(Vector3 vec3)
        {
            return vec3.x + "," + vec3.y + "," + vec3.z;
        }

        private static string QuaternionToString(Quaternion quat)
        {
            return quat.x + "," + quat.y + "," + quat.z + "," + quat.w;
        }

        public void Undo()
        {
            this.transform.localPosition = beforeCallFrameTransform.localPosition;
            this.transform.localRotation = beforeCallFrameTransform.localRotation;
            this.transform.localScale = beforeCallFrameTransform.localScale;
        }
    }
}