using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityVolumeRendering;

namespace HoloAuopsy
{
    public enum ControlState
    {
        INACTIVE, READY, MOVE_CP, ADD_CP, DELETE_CP
    }
    public class TransferFunctionUtils
    {
        public const float OFFSET = 0.5f;
        private static Color[] fillColors;
        private static int width;
        private static int height;
        public static void ChangeGrabbingComponentStates(GameObject target, bool objectManipulatorEnabled, bool constraintManagerEnabled, bool boxColliderEnabled)
        {
            target.GetComponent<ObjectManipulator>().enabled = objectManipulatorEnabled;
            target.GetComponent<ConstraintManager>().enabled = constraintManagerEnabled;
            target.GetComponent<BoxCollider>().enabled = boxColliderEnabled;
        }
        public static void ChangeAddCPComponentStates(GameObject target, bool nearInteractionTouchable, bool touchHandler, bool boxCollider)
        {
            target.GetComponent<NearInteractionTouchable>().enabled = nearInteractionTouchable;
            target.GetComponent<TouchHandler>().enabled = touchHandler;
            target.GetComponent<BoxCollider>().enabled = boxCollider;
        }

        public static void CreateAlphaControlPoints(Transform parent, GameObject cpPrefab, TransferFunction tf, UnityAction<ManipulationEventData> eventListener)
        {
            parent.DetachChildren();
            var alphaControlPoints = tf.alphaControlPoints;
            foreach (TFAlphaControlPoint cp in alphaControlPoints)
            {
                var obj = Object.Instantiate(cpPrefab, parent);
                obj.transform.localPosition = new Vector3(cp.dataValue - OFFSET, cp.alphaValue - OFFSET, 0.0f);
                obj.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener(eventListener);
            }
        }
        public static void CreateColorControlPoints(Transform parent, GameObject cpPrefab, TransferFunction tf, UnityAction<ManipulationEventData> eventListener)
        {
            parent.DetachChildren();
            var colorControlPoints = tf.colourControlPoints;
            
            int i = 0;
            foreach (TFColourControlPoint cp in colorControlPoints)
            {
                var color = cp.colourValue;
                color.a = 1;
                var obj = Object.Instantiate(cpPrefab, parent);
                obj.transform.localPosition = new Vector3(cp.dataValue - OFFSET, 0.0f, 0.0f);
                obj.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
                obj.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener(eventListener);
                i++;
            }
        }


        public static void UpdateTextureByColorCP(int objNum, Transform parent, TransferFunction tf, bool immediateApply = true)
        {
            List<TFColourControlPoint> colorControlPoints = tf.colourControlPoints;
            int cpCount = colorControlPoints.Count;

            if (cpCount != parent.childCount)
            {
                throw new System.ArgumentException("Size of arguments is not matched. Transfer function color control points count is " + cpCount + " while Color Bar's children count is " + parent.childCount);
            }
            
            Transform cpObj;
            cpObj = parent.GetChild(objNum);
            if (colorControlPoints[objNum].dataValue != cpObj.localPosition.x)
            {
                tf.colourControlPoints[objNum] = new TFColourControlPoint(cpObj.localPosition.x + OFFSET, tf.colourControlPoints[objNum].colourValue);
                if (immediateApply) tf.GenerateTexture();
            }
        }
        public static void UpdateTextureByAlphaCP(int objNum, Transform parent, TransferFunction tf, bool immediateApply = true)
        {
            List<TFAlphaControlPoint> alphaControlPoints = tf.alphaControlPoints;
            int cpCount = alphaControlPoints.Count;

            if (cpCount != parent.childCount)
            {
                throw new System.ArgumentException("Size of arguments is not matched. Transfer function alpha control points count is " + cpCount + " while Intensity Histogram's children count is " + parent.childCount);
            }
            
            Transform cpObj;
            cpObj = parent.GetChild(objNum);
            if (alphaControlPoints[objNum].dataValue != cpObj.localPosition.x || alphaControlPoints[objNum].alphaValue != cpObj.localPosition.y)
            {
                tf.alphaControlPoints[objNum] = new TFAlphaControlPoint(cpObj.localPosition.x + OFFSET, cpObj.localPosition.y + OFFSET); ;
                if (immediateApply) tf.GenerateTexture();
            }
        }
        public static void AddAlphaControlPoint(Vector3 pos, Transform parent, GameObject cpPrefab, TransferFunction tf, UnityAction<ManipulationEventData> eventListener)
        {
            List<TFAlphaControlPoint> alphaControlPoints = tf.alphaControlPoints;

            //Add point to parent
            var obj = Object.Instantiate(cpPrefab, parent);
            obj.transform.localPosition = new Vector3(pos.x, pos.y, 0.0f);
            obj.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener(eventListener);

            //Add AlphaControlPoint to Transfer Function
            tf.AddControlPoint(new TFAlphaControlPoint(pos.x + OFFSET, pos.y + OFFSET));

            tf.GenerateTexture();
        }
        public static void RemoveAlphaControlPoint(GameObject obj, TransferFunction tf)
        {
            List<TFAlphaControlPoint> alphaControlPoints = tf.alphaControlPoints;
            int cpCount = alphaControlPoints.Count;

            if (cpCount != obj.transform.parent.childCount)
            {
                throw new System.ArgumentException("Size of arguments is not matched. Transfer function alpha control points count is " + cpCount + " while Intensity Histogram's children count is " + obj.transform.parent.childCount);
            }

            bool aCPRemoved = false;
            Vector3 objPos = obj.transform.localPosition;

            for (int i = 0; i < cpCount; i++)
            {
                if (IsPointsNear(objPos, alphaControlPoints[i]))
                {
                    //Debug.Log("Alpha ControlPoint Removed = (" + alphaControlPoints[i].dataValue + "," + alphaControlPoints[i].alphaValue + ")");
                    tf.alphaControlPoints.RemoveAt(i);
                    aCPRemoved = true;
                    break;
                }
            }

            if (aCPRemoved)
            {
                obj.transform.SetParent(null);
                Object.Destroy(obj);
            }
            tf.GenerateTexture();
        }


        public static bool IsPointsNear(Vector3 objPosition, TFAlphaControlPoint cp)
        {
            if (Mathf.Abs(objPosition.x + OFFSET - cp.dataValue) < 0.001 && Mathf.Abs(objPosition.y + OFFSET - cp.alphaValue) < 0.001) return true;
            return false;
        }

        public static Vector4[] ConvertTFControlPointsToVector()
        {
            return null;
        }

        public static Texture2D MixColorToTexture(Texture2D main, Color32[] overlay)
        {
            Texture2D resTex = new Texture2D(main.width, main.height);
            resTex.SetPixels32(overlay, 0);
            return null;
        }

        /// <summary>
        /// Adds first and last alpha control points (if missed) and sorts the lists by intensity
        /// </summary>
        private static void NormalizeControlPoints(List<TFAlphaControlPoint> list)
        {
            list.Sort((a, b) => a.dataValue.CompareTo(b.dataValue));
            if (list[list.Count - 1].dataValue != 1.0f) list.Add(new TFAlphaControlPoint(1f, 1f));
            if (list[0].dataValue != 0.0f) list.Add(new TFAlphaControlPoint(0f, 0f));
            list.Sort((a, b) => a.dataValue.CompareTo(b.dataValue));
        }

        public static void DrawAlphaControlPoints(List<TFAlphaControlPoint> controlPoints, Texture2D texture)
        {
            //Initialise a fit color array
            if (fillColors == null || fillColors.Length != texture.width * texture.height)
            {
                fillColors = new Color[texture.width * texture.height];
            }


            //Clean Color array
            for (int i = 0; i < fillColors.Length; i++)
            {
                fillColors[i].r = 0;
                fillColors[i].g = 0;
                fillColors[i].b = 0;
                fillColors[i].a = 0;
            }

            //Draw Control Point
            NormalizeControlPoints(controlPoints);

            //Draw Lines between control points
            TFAlphaControlPoint cp1, cp2;
            for (int i = 1; i < controlPoints.Count; i++)
            {
                cp1 = controlPoints[i - 1];
                cp2 = controlPoints[i];
            }

            //Draw box for each control point

            //Apply to texture
            texture.SetPixels(fillColors);
            texture.Apply();
        }

        private static void DrawLine(float x1, float y1, float x2, float y2, Color[] colors)
        {
            float x, y;
            float dy = y2 - y1;
            float dx = x2 - x1;
            float m = dy / dx;
            float m2 = dx / dy;
            float dy_inc = -1;

            if (dy < 0)
                dy = 1;

            float dx_inc = 1;
            if (dx < 0)
                dx = -1;

            int index;

            if (Mathf.Abs(dy) > Mathf.Abs(dx))
            {
                for (y = y2; y < y1; y += dy_inc)
                {
                    x = x1 + (y - y1) * m;

                    index = (int)(x) + (int)(y) * width;
                    if (index >= 0 && index <= colors.Length)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        Debug.Log("Some miscalculation on index " + index
                            + ", x=" + x + ",y=" + y + ",width=" + width);
                    }
                }
            }
            else
            {
                for (x = x1; x < x2; x += dx_inc)
                {
                    y = y1 + (x - x1) * m2;

                    index = (int)(x) + (int)(y) * width;
                    if (index >= 0 && index <= colors.Length)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        Debug.Log("Some miscalculation on index " + index
                            + ", x=" + x + ",y=" + y + ",width=" + width);
                    }
                }
            }
        }
    }
}