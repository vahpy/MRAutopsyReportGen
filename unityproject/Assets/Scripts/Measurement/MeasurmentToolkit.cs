using HoloAutopsy.Utils;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.Measurement
{
    public class MeasurmentToolkit : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private List<MultiLine> measures;

        [SerializeField]
        private CustomisedHandGesture cursorLeft;
        [SerializeField]
        private CustomisedHandGesture cursorRight;

        [SerializeField]
        private GameObject pointPrefab;
        [SerializeField]
        private GameObject labelPrefab;
        [SerializeField]
        private GameObject dialogeBox;
        [SerializeField]
        private LineRenderer ruler;
        [SerializeField]
        private bool createNewLine;
        private bool lastCreateNewLineState;

        private Vector3 lastChooseLinePos;

        private TMPro.TextMeshPro rulerLabel;

        private bool last_isDrawingMeasure;
        private bool isDrawingMeasure;

        private bool isActive;

        private MultiLine currentMultiLine;

        private float time_counter;

        private Vector3 selectedCursorPosition;
        private bool optionSelected;
        private bool lastHandTriggered; //false-left, true-right
        void Start()
        {
            measures = new List<MultiLine>();
            time_counter = Time.fixedUnscaledTime;
        }

        void OnEnable()
        {
            lastCreateNewLineState = createNewLine;
            dialogeBox.SetActive(false);
            selectedCursorPosition = Vector3.zero;
            isDrawingMeasure = false;
            last_isDrawingMeasure = !isDrawingMeasure;
            optionSelected = false;
            currentMultiLine = null;
            if (ruler != null)
            {
                ruler.startWidth = 0.001f;
                ruler.endWidth = 0.001f;
                rulerLabel = ruler.GetComponentInChildren<TMPro.TextMeshPro>();
                if (rulerLabel != null) rulerLabel.transform.localPosition = Vector3.zero;
            }
        }

        void Update()
        {
            if(lastCreateNewLineState != createNewLine)
            {
                lastCreateNewLineState=createNewLine;
                currentMultiLine=null;
            }
            DrawingStateChangeObjectControl();
            if (!isDrawingMeasure)
            {
                //interactive parts
                foreach (MultiLine multi in measures)
                {
                    multi.EachFrameUpdate();
                }
                return;
            }

            foreach (MultiLine multi in measures)
            {
                multi.UpdateLineMovements();
                multi.UpdateControlPointScale(cursorLeft.transform.position, cursorRight.transform.position);
                multi.EachFrameUpdate();
            }
            //show ruler
            MixedRealityPose pose1, pose2;
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Left, out pose1) && HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out pose2))
            {
                if (ruler != null)
                {
                    ruler.gameObject.SetActive(true);
                    ruler.transform.position = (pose1.Position + pose2.Position) / 2.0f;
                    ruler.SetPositions(new Vector3[] { pose1.Position, pose2.Position });
                }
                var dist = Vector3.Distance(pose1.Position, pose2.Position);
                string labelTxt = (dist * 100).ToString("####.#") + " cm";
                if (rulerLabel != null)
                {
                    AlwaysLookAtCamera.AdaptRotation(rulerLabel.transform);
                    rulerLabel.text = labelTxt;
                    if (dist < 0.1)
                    {
                        rulerLabel.transform.localScale = Vector3.one / 6;
                    }
                    else
                    {
                        rulerLabel.transform.localScale = Vector3.one / 2;
                    }
                }
            }
            else
            {
                ruler?.gameObject.SetActive(false);
            }
        }

        private void DrawingStateChangeObjectControl()
        {
            if (isDrawingMeasure && isDrawingMeasure != last_isDrawingMeasure)
            {
                //Enable Control Points Movement
                foreach (MultiLine multi in measures)
                {
                    foreach (GameObject obj in multi.GetControlPoints())
                    {
                        obj.GetComponent<BoxCollider>().enabled = true;
                        obj.GetComponent<NearInteractionGrabbable>().enabled = true;
                        obj.GetComponent<ObjectManipulator>().enabled = true;
                    }
                }
                cursorLeft.gameObject.SetActive(true);
                cursorRight.gameObject.SetActive(true);
            }
            else if (isDrawingMeasure != last_isDrawingMeasure)
            {
                if (ruler != null && ruler.gameObject.activeSelf)
                    ruler.gameObject.SetActive(false);
                //Disable Control Points Movement
                foreach (MultiLine multi in measures)
                {
                    foreach (GameObject obj in multi.GetControlPoints())
                    {
                        obj.GetComponent<BoxCollider>().enabled = false;
                        obj.GetComponent<NearInteractionGrabbable>().enabled = false;
                        obj.GetComponent<ObjectManipulator>().enabled = false;
                    }
                }
                cursorLeft.gameObject.SetActive(false);
                cursorRight.gameObject.SetActive(false);
            }
            last_isDrawingMeasure = isDrawingMeasure;
        }

        public void MovePointLeft()
        {
            lastHandTriggered = false;
            //MovePoint(cursorLeft.transform.position);
        }
        public void MovePointRight()
        {
            lastHandTriggered = true;
            //MovePoint(cursorRight.transform.position);
        }

        public void AddPointLeft()
        {
            lastHandTriggered = false;
            lastChooseLinePos = cursorLeft.transform.position;
            AddPoint(cursorLeft.transform.position);
        }

        public void DeletePointLeft()
        {
            lastHandTriggered = false;
            DeletePoint(cursorLeft.transform.position);
        }

        

        public void AddPointRight()
        {
            lastHandTriggered = true;
            lastChooseLinePos = cursorRight.transform.position;
            AddPoint(cursorRight.transform.position);
        }

        public void DeletePointRight()
        {
            lastHandTriggered = true;
            DeletePoint(cursorRight.transform.position);
        }

        public void SelectCurrentLine(Vector3 position)
        {
            if (Time.fixedUnscaledTime > time_counter + 0.5f)
            {
                time_counter = Time.fixedUnscaledTime;
                foreach (MultiLine multi in measures)
                {
                    foreach (GameObject obj in multi.GetControlPoints())
                    {
                        if (obj.GetComponent<BoxCollider>().bounds.Contains(position))
                        {
                            currentMultiLine = multi;
                            return;
                        }
                    }
                }
            }
        }
        public void MovePoint(Vector3 position)
        {
            if (Time.fixedUnscaledTime > time_counter + 0.5f)
            {
                time_counter = Time.fixedUnscaledTime;
                foreach (MultiLine multi in measures)
                {
                    foreach (GameObject obj in multi.GetControlPoints())
                    {
                        if (obj.GetComponent<BoxCollider>().bounds.Contains(position))
                        {
                            currentMultiLine = multi;
                            return;
                        }
                    }
                }
            }
        }
        public void AddPoint(Vector3 position)
        {
            if (!isDrawingMeasure) return;
            Vector3 addPosition = position;

            if (dialogeBox.activeSelf && optionSelected)
            {
                addPosition = selectedCursorPosition;
            }
            else
            {
                bool isPosOnExistingCP = false;
                bool openDialoge = false;
                foreach (MultiLine multi in measures)
                {
                    foreach (GameObject obj in multi.GetControlPoints())
                    {
                        if (obj.GetComponent<BoxCollider>().bounds.Contains(position))
                        {
                            openDialoge = true;
                            isPosOnExistingCP = true;
                        }
                    }
                }
                if (currentMultiLine != null && currentMultiLine.GetControlPoints().Count >= 2)
                {
                    openDialoge = true;
                }

                if (openDialoge)
                {
                    selectedCursorPosition = position;
                    //Show the options for adding or deleting an object
                    dialogeBox.transform.position = position + Vector3.up * 0.1f;
                    AlwaysLookAtCamera.AdaptRotation(dialogeBox.transform);
                    
                    foreach(var comp in dialogeBox.GetComponentsInChildren<Transform>(includeInactive: true))
                    {
                        if(comp.gameObject.name.ToLower().Contains("delete"))
                        {
                            comp.gameObject.SetActive(isPosOnExistingCP);
                            break;
                        }
                    }
                    foreach (var comp in dialogeBox.GetComponentsInChildren<Transform>(includeInactive: true))
                    {
                        if (comp.gameObject.name.ToLower().Contains("choose"))
                        {
                            comp.gameObject.SetActive(isPosOnExistingCP);
                            break;
                        }
                    }

                    dialogeBox.SetActive(true);
                    return;
                }
            }
            if (currentMultiLine == null)
            {
                currentMultiLine = new MultiLine();
                measures.Add(currentMultiLine);
                isDrawingMeasure = true;
            }
            Debug.Log(addPosition);
            currentMultiLine.AddPoint(addPosition, pointPrefab, labelPrefab, this.transform, true);
        }
        public void DeletePoint(Vector3 position)
        {
            if (!isDrawingMeasure) return;
            Vector3 deletePos = position;
            if (dialogeBox.activeSelf)
            {
                deletePos = selectedCursorPosition;
            }
            foreach (MultiLine multi in measures)
            {
                foreach (GameObject obj in multi.GetControlPoints())
                {
                    if (obj.GetComponent<BoxCollider>().bounds.Contains(deletePos))
                    {
                        multi.DeletePoint(obj);
                        return;
                    }
                }
            }
        }
        public void DialogeAddOption()
        {
            optionSelected = true;
            if (lastHandTriggered) AddPointRight();
            else AddPointLeft();
            dialogeBox.SetActive(false);
            optionSelected = false;
        }
        public void DialogeDeleteOption()
        {
            optionSelected = true;
            if (lastHandTriggered) DeletePointRight();
            DeletePointLeft();
            dialogeBox.SetActive(false);
            optionSelected = false;
        }
        public void DialogeChooseLineOption()
        {
            optionSelected = true;
            SelectCurrentLine(lastChooseLinePos);
            dialogeBox.SetActive(false);
            optionSelected = false;
        }
        public void DialogeCancelOption()
        {
            optionSelected = true;
            dialogeBox.SetActive(false);
            optionSelected = false;
        }
        public void PerformAction()
        {
            Debug.Log("Triggered");
        }

        public void DeletePoint(GameObject controlPoint)
        {

        }

        public void EndDrawing()
        {

        }

        public void EndThisLine()
        {

        }

        public void ToggleState()
        {
            currentMultiLine = null;
        }
        public void ToggleDrawing()
        {
            isDrawingMeasure = !isDrawingMeasure;
        }
        public bool GetDrawingState()
        {
            return isDrawingMeasure;
        }
        public void ToggleActivation()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    // Supplementary Classes 
    public class MultiLine
    {
        [SerializeField]
        private List<GameObject> controlPoints;
        [SerializeField]
        private GameObject multiLine;
        [SerializeField]
        private List<GameObject> lines;

        [SerializeField]
        private List<Vector3> cpLastPositions;


        private Vector3 defaultVertexPrefabScale = new Vector3(0.005f, 0.005f, 0.005f);
        private Vector3 maxVertexPrefabScale = new Vector3(0.02f, 0.02f, 0.02f);
        public MultiLine()
        {
            controlPoints = new List<GameObject>();
            cpLastPositions = new List<Vector3>();
            lines = new List<GameObject>();
        }

        public void UpdateControlPointScale(Vector3 point1, Vector3 point2)
        {
            ScaleVerticesRelativeToCloseness(point1, point2);
        }

        public void UpdateLineMovements()
        {
            bool edgeMoved = false;
            int pointCount = controlPoints.Count;
            for (int i = 0; i < pointCount; i++)
            {
                if (controlPoints[i].transform.localPosition != cpLastPositions[i])
                {
                    edgeMoved = true;
                    //Update line
                    cpLastPositions[i] = controlPoints[i].transform.localPosition;
                    if (i >= 1)
                    {
                        lines[i - 1].transform.position = (controlPoints[i - 1].transform.position + controlPoints[i].transform.position) / 2;
                        lines[i - 1].GetComponent<LineRenderer>().SetPositions(new Vector3[] { controlPoints[i - 1].transform.position, controlPoints[i].transform.position });
                    }
                    if (i + 1 < cpLastPositions.Count)
                    {
                        lines[i].transform.position = (controlPoints[i].transform.position + controlPoints[i + 1].transform.position) / 2;
                        lines[i].GetComponent<LineRenderer>().SetPositions(new Vector3[] { controlPoints[i].transform.position, controlPoints[i + 1].transform.position });
                    }
                }
            }
            if (edgeMoved) UpdateEdgeLabelValues();

            //Vector3[] positions = new Vector3[controlPoints.Count];
            //for (int i = 0; i < controlPoints.Count; i++)
            //{
            //    positions[i] = controlPoints[i].transform.localPosition;
            //}
            //LineRenderer lineRen = line.GetComponent<LineRenderer>();
            //lineRen.positionCount = positions.Length;
            //lineRen?.SetPositions(positions);
        }
        public void EachFrameUpdate()
        {
            foreach (var line in lines)
            {
                AlwaysLookAtCamera.AdaptRotation(line.GetComponentInChildren<Transform>());
            }
        }
        private void UpdateEdgeLabelValues()
        {
            int i = 0;

            foreach (var line in lines)
            {
                var label = line.GetComponentInChildren<TMPro.TextMeshPro>();
                if (label != null)
                {
                    Vector3[] positions = new Vector3[2];
                    line.GetComponent<LineRenderer>().GetPositions(positions);
                    var dist = Vector3.Distance(positions[0], positions[1]);
                    string labelTxt = (dist * 100).ToString("####.#") + " cm";
                    label.text = labelTxt;
                }
                i++;
            }
        }

        public void DeletePoint(GameObject obj)
        {
            if (lines.Count > 0)
            {
                GameObject line = lines[lines.Count - 1];
                lines.Remove(line);
                line.transform.SetParent(null);
                Object.Destroy(line);
            }
            controlPoints.Remove(obj);
            cpLastPositions = new List<Vector3>();
            foreach (var cp in controlPoints)
            {
                cpLastPositions.Add(Vector3.zero);
            }
            obj.transform.SetParent(null);
            Object.Destroy(obj);
            UpdateLineMovements();
            UpdateEdgeLabelValues();
        }

        public void AddPoint(Vector3 position, GameObject pointPrefab, GameObject labelPrefab, Transform parent, bool worldSpace)
        {
            if (controlPoints.Count == 0)
            {
                multiLine = new GameObject("MultiLine");
                multiLine.transform.parent = parent;
                multiLine.transform.localPosition = Vector3.zero;
                multiLine.transform.localScale = Vector3.one;
                multiLine.transform.localRotation = Quaternion.identity;
            }
            Vector3 localPosition = position;
            Vector3 worldPosition = position;
            if (worldSpace)
            {
                localPosition = multiLine.transform.worldToLocalMatrix.MultiplyPoint(position);
            }
            else
            {
                worldPosition = multiLine.transform.localToWorldMatrix.MultiplyPoint(position); ;
            }
            GameObject controlPoint = Object.Instantiate(pointPrefab, multiLine.transform, true);
            controlPoint.transform.position = worldPosition;
            
            controlPoints.Add(controlPoint);
            cpLastPositions.Add(localPosition);

            if (controlPoints.Count >= 2)
            {
                GameObject line = new GameObject("line" + (multiLine.transform.childCount + 1));
                line.transform.SetParent(multiLine.transform);
                LineRenderer lineRen = line.AddComponent<LineRenderer>();
                lineRen.positionCount = 2;


                lineRen.SetPositions(new Vector3[] { controlPoints[controlPoints.Count - 2].transform.position, controlPoints[controlPoints.Count - 1].transform.position });
                lineRen.startWidth = 0.003f;
                lineRen.endWidth = 0.003f;
                lineRen.startColor = Color.cyan;
                lineRen.endColor = Color.cyan;

                line.transform.position = (controlPoints[controlPoints.Count - 2].transform.position + controlPoints[controlPoints.Count - 1].transform.position) / 2;

                lines.Add(line);

                //add label
                GameObject label = Object.Instantiate(labelPrefab, line.transform, false);
                var dist = Vector3.Distance(controlPoints[controlPoints.Count - 2].transform.position, controlPoints[controlPoints.Count - 1].transform.position);
                label.GetComponent<TMPro.TextMeshPro>().text = (dist * 100).ToString("####.#") + " cm";
                label.transform.localPosition = Vector3.zero;
                //UpdateLabelPositions();
            }
            UpdateControlPointScale(controlPoint.transform.position, controlPoint.transform.position);
        }
        private void ScaleVerticesRelativeToCloseness(Vector3 point1, Vector3 point2)
        {
            foreach (var vertex in controlPoints)
            {
                var dist = Mathf.Min(Vector3.Distance(vertex.transform.position, point1),
                    Vector3.Distance(vertex.transform.position, point2));
                if (dist > 0.5) vertex.transform.localScale = defaultVertexPrefabScale;
                else
                {
                    vertex.transform.localScale = Vector3.Lerp(maxVertexPrefabScale, defaultVertexPrefabScale, -Mathf.Pow(2 * dist - 1, 2) + 1);
                }
                vertex.transform.hasChanged = false;
            }
        }

        public Transform GetControlPointTransform(int index)
        {
            return controlPoints[index].transform;
        }

        public List<GameObject> GetControlPoints()
        {
            return controlPoints;
        }
    }
}