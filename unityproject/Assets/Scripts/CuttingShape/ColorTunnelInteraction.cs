using HoloAutopsy.ColorTunnel;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace HoloAutopsy.CuttingShape
{
    //[ExecuteInEditMode]
    public class ColorTunnelInteraction : MonoBehaviour
    {
        [SerializeField]
        private UnityVolumeRendering.VolumeRenderedObject volRenObj;
        [SerializeField]
        private PersistColorTunnelRunner persistentTunnel;
        [SerializeField]
        private TMPro.TextMeshPro minLabel;
        [SerializeField]
        private TMPro.TextMeshPro maxLabel;
        [SerializeField]
        private Transform tunnelSphere;
        [SerializeField, Range(0.01f, 0.04f)]
        private float jointThreshold = 0.025f;
        //User study controls
        [SerializeField]
        private bool enableLeftTapForRange = false;
        [SerializeField]
        private Material[] tunnelShpereMat; // 0 - Non-persistent mat, 1- persistent mat

        
        //Mat
        private Material minRangeMat;
        private Material maxRangeMat;
        private float timeChangingMat;
        private bool matChangeDir;


        //States
        private bool enable;
        private bool adjustRadius;

        private bool minRangeAdjustment;
        private bool maxRangeAdjustment;

        private bool lastPersistentTunnelEnabled;
        // right click
        private float lastRightClickPressTime;
        private bool rightClicked;
        // left click
        private float lastLeftClickPressTime;
        private bool leftClicked;

        //Tap to middle joint
        private bool holdLeftTap;
        private bool holdRightTap;


        private Vector3? iMRight;
        private Vector3? iMLeft;
        private Vector3? iTRight;
        private Vector3? iTLeft;
        //private Vector3? mMRight;
        //private Vector3? mMLeft;
        private Vector3? tTRight;
        private Vector3? tTLeft;
        private Vector3? palmUpLeft;
        private Vector3? palmUpRight;
        private Vector3? palmFoLeft;
        private Vector3? palmFoRight;

        const float minChangeStateTimeThreshold = 0.5f;

        private void OnEnable()
        {
            this.transform.position = Vector3.up * 1000;
            adjustRadius = false;
            //adjustRange = false;
            lastRightClickPressTime = 0;
            lastLeftClickPressTime = 0;
            minLabel.text = "0.0";
            maxLabel.text = "1.0";
            minLabel.GetComponent<MeshRenderer>().enabled = false;
            maxLabel.GetComponent<MeshRenderer>().enabled = false;

            holdLeftTap = false;
            holdRightTap = false;

            // Activation state
            enable = false;
            if (volRenObj.GetColorTunnelingEnabled() || volRenObj.GetPersistColorTunnelingEnabled())
            {
                tunnelSphere.GetComponent<MeshRenderer>().enabled = true;
                minLabel.GetComponent<MeshRenderer>().enabled = true;
                maxLabel.GetComponent<MeshRenderer>().enabled = true;
                enable = true;
            }
            else
            {
                tunnelSphere.GetComponent<MeshRenderer>().enabled = false;
                minLabel.GetComponent<MeshRenderer>().enabled = true;
                maxLabel.GetComponent<MeshRenderer>().enabled = true;
            }
            minRangeMat = minLabel.GetComponent<MeshRenderer>().sharedMaterial;
            maxRangeMat = maxLabel.GetComponent<MeshRenderer>().sharedMaterial;
            //sphereTunnelMat = tunnelSphere.GetComponent<MeshRenderer>().sharedMaterial;
            minRangeAdjustment = false;
            maxRangeAdjustment = false;
            if (volRenObj != null) lastPersistentTunnelEnabled = !volRenObj.GetPersistColorTunnelingEnabled();
            else lastPersistentTunnelEnabled = false;
            //adjustRange = false;
            if (persistentTunnel == null)
            {
                persistentTunnel = this.GetComponent<PersistColorTunnelRunner>();
            }
        }
        void Update()
        {
            //Material Control
            MinMaxMaterialRange();
            //Persisent/non-presistent material
            PresisentModeMaterialControl();

            // Handle Activation states
            if (!volRenObj.GetColorTunnelingEnabled() && !volRenObj.GetPersistColorTunnelingEnabled())
            {
                if (enable)
                {
                    enable = false;
                    tunnelSphere.GetComponent<MeshRenderer>().enabled = false;
                    minLabel.GetComponent<MeshRenderer>().enabled = false;
                    maxLabel.GetComponent<MeshRenderer>().enabled = false;
                    adjustRadius = false;
                    //adjustRange = false;
                    minRangeAdjustment = false;
                    maxRangeAdjustment = false;
                }
                return;
            }
            if (!enable)
            {
                enable = true;
                tunnelSphere.GetComponent<MeshRenderer>().enabled = true;
                minLabel.GetComponent<MeshRenderer>().enabled = true;
                maxLabel.GetComponent<MeshRenderer>().enabled = true;
            }

            // Pose getters
            JointsPos();

            // Change state
            StateTransition();

            // Rotation
            AlwaysLookAtCamera.AdaptRotation(this.transform);

#if UNITY_EDITOR
            volRenObj.SetColorTunnelCenter(this.transform.position);
#endif

            // Center adjustment
            if (adjustRadius)
            {
                var center = GetCenter();
                if (center != null)
                {
                    volRenObj.SetColorTunnelCenter((Vector3)center);
                    this.transform.position = (Vector3)center;
                }

                // Radius adjustment
                var radius = GetRadius();
                if (radius != null)
                {
                    volRenObj.SetColorTunnelRadius((float)radius);
                    var notNullRadius = (float)radius * 5.0f;
                    tunnelSphere.localScale = new Vector3(notNullRadius, notNullRadius, notNullRadius);
                }
            }

            // Range Getter
            if (minRangeAdjustment && maxRangeAdjustment)
            {
                var range = GetRange();
                if (range != null)
                {
                    var min = ((Vector2)range).x;
                    minLabel.text = min.ToString("0.00");
                    var max = ((Vector2)range).y;
                    maxLabel.text = max.ToString("0.00");
                    var temp = min;
                    if (min > max)
                    {
                        min = max;
                        max = temp;
                    }
                    volRenObj.SetColorTunnelRange(min, max);
                }
            }
            else if (minRangeAdjustment || maxRangeAdjustment)
            {
                var value = GetRangeOneHand();
                if (value != null)
                {
                    var min = float.Parse(minLabel.text);
                    var max = float.Parse(maxLabel.text);
                    if (minRangeAdjustment)
                    {
                        min = (float)value;
                        minLabel.text = min.ToString("0.00");
                    }
                    else if (maxRangeAdjustment)
                    {
                        max = (float)value;
                        maxLabel.text = max.ToString("0.00");
                    }
                    var temp = min;
                    if (min > max)
                    {
                        min = max;
                        max = temp;
                    }
                    volRenObj.SetColorTunnelRange(min, max);
                }
            }
        }
        private void CheckChangeToPersistent()
        {
            if (volRenObj == null) return;
            if (leftClicked && rightClicked && !volRenObj.GetPersistColorTunnelingEnabled())
            {
                volRenObj.SetPersistColorTunnelingEnabled(true);
            }
            if(leftClicked && rightClicked)
            {
                persistentTunnel.ForceRun();
                persistentTunnel.SetPersistentActive(true);
            }
            else
            {
                persistentTunnel.SetPersistentActive(false);
            }
        }
        public void LeftPinchClick()
        {
            leftClicked = true;
            CheckChangeToPersistent();
        }
        public void RightPinchClick()
        {
            rightClicked = true;
            CheckChangeToPersistent();
        }
        public void LeftPinchRelease()
        {
            leftClicked = false;
            CheckChangeToPersistent();
        }
        public void RightPinchRelease()
        {
            rightClicked = false;
            CheckChangeToPersistent();
        }
        public void ToggleMinRangeAdjustment()
        {
            //if both are active, this tap means to edit this one not disabling it
            if (minRangeAdjustment && maxRangeAdjustment)
            {
                maxRangeAdjustment = false;
            }
            else minRangeAdjustment = !minRangeAdjustment;
            if (minRangeAdjustment)
            {
                //adjustRange = true;
                maxRangeAdjustment = false;
            }
        }
        public void ToggleMaxRangeAdjustment()
        {
            //if both are active, this tap means to edit this one not disabling it
            if (minRangeAdjustment && maxRangeAdjustment)
            {
                minRangeAdjustment = false;
            }
            else maxRangeAdjustment = !maxRangeAdjustment;
            if (maxRangeAdjustment)
            {
                //adjustRange = true;
                minRangeAdjustment = false;
            }
        }

        private void PresisentModeMaterialControl()
        {
            if (volRenObj == null) return;
            var currentState = persistentTunnel.GetPersistentActivationState();

            if (currentState != lastPersistentTunnelEnabled)
            {
                lastPersistentTunnelEnabled = currentState;

                if (currentState) tunnelSphere.GetComponent<MeshRenderer>().sharedMaterial = tunnelShpereMat[1];
                else tunnelSphere.GetComponent<MeshRenderer>().sharedMaterial = tunnelShpereMat[0];
            }
        }

        private void MinMaxMaterialRange()
        {
            Color c = new Color(Mathf.Lerp(1, 0, timeChangingMat * 2), Mathf.Lerp(0, 1, timeChangingMat * 2), 0);
            if (minRangeAdjustment) minRangeMat.SetColor("_OutlineColor", c);
            else minRangeMat.SetColor("_OutlineColor", Color.red);
            if (maxRangeAdjustment) maxRangeMat.SetColor("_OutlineColor", c);
            else maxRangeMat.SetColor("_OutlineColor", Color.red);

            //Time
            if (matChangeDir) timeChangingMat += Time.unscaledDeltaTime;
            else timeChangingMat -= Time.unscaledDeltaTime;
            if (timeChangingMat > 0.5)
            {
                matChangeDir = false;
            }
            else if (timeChangingMat < 0)
            {
                matChangeDir = true;
            }
        }
        //
        private float? GetRangeOneHand()
        {
            if (palmUpRight != null && palmFoRight != null)
            {
                float value;
                Vector3 pRight = (Vector3)palmUpRight;

                var originRightUp = Vector3.Cross((Vector3)palmFoRight, Camera.main.transform.right);
                value = Vector3.Dot(pRight, originRightUp);
                value = -0.5f * value + 0.5f;
                value = Mathf.Clamp01(value);

                return value;
            }
            else if (palmUpLeft != null && palmFoLeft != null)
            {
                float value;
                Vector3 pLeft = (Vector3)palmUpLeft;

                var originLeftUp = Vector3.Cross((Vector3)palmFoLeft, Camera.main.transform.right);
                value = Vector3.Dot(pLeft, originLeftUp);
                value = 0.5f * value + 0.5f;
                value = Mathf.Clamp01(value);

                return value;
            }
            else
            {
                return null;
            }
        }
        private Vector2? GetRange()
        {
            if (palmUpLeft == null || palmUpRight == null || palmFoLeft == null || palmFoRight == null) return null;
            Vector3 pLeft = (Vector3)palmUpLeft;
            Vector3 pRight = (Vector3)palmUpRight;
            float x, y;

            var originLeftUp = Vector3.Cross((Vector3)palmFoLeft, Camera.main.transform.right);
            var originRightUp = Vector3.Cross((Vector3)palmFoRight, Camera.main.transform.right);
            x = Vector3.Dot(pLeft, originLeftUp);
            y = Vector3.Dot(pRight, originRightUp);
            //if (Camera.main != null)
            //{
            //    Vector3 camUp = Camera.main.transform.up;
            //    x = Vector3.Dot(pLeft, camUp);
            //    y = Vector3.Dot(pRight, camUp);
            //}
            //else
            //{
            //    x = Vector3.Dot(pLeft, Vector3.up);
            //    y = Vector3.Dot(pRight, Vector3.up);
            //}
            x = 0.5f * x + 0.5f;
            y = -0.5f * y + 0.5f;
            x = Mathf.Clamp01(x);
            y = Mathf.Clamp01(y);
            return new Vector2(x, y);
        }

        public Vector3? GetCenter()
        {
            if (iTRight == null || iTLeft == null) return null;
            return (iTRight + iTLeft) / 2;
        }
        public float? GetRadius()
        {
            if (iTRight == null || iTLeft == null) return null;
            return Vector3.Distance((Vector3)iTRight, (Vector3)iTLeft) / 2;
        }

        private void JointsPos()
        {
            iMRight = null;
            iMLeft = null;
            iTRight = null;
            iTLeft = null;
            //mMLeft = null;
            //mMRight = null;
            tTRight = null;
            tTLeft = null;
            palmUpLeft = null;
            palmUpRight = null;
            palmFoLeft = null;
            palmFoRight = null;

            MixedRealityPose pose;
            //if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleMiddleJoint, Handedness.Left, out pose))
            //    mMLeft = pose.Position;

            //if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleMiddleJoint, Handedness.Right, out pose))
            //    mMRight = pose.Position;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out pose))
                iTLeft = pose.Position;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out pose))
                iTRight = pose.Position;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, Handedness.Left, out pose))
                iMLeft = pose.Position;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, Handedness.Right, out pose))
                iMRight = pose.Position;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Left, out pose))
                tTLeft = pose.Position;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out pose))
                tTRight = pose.Position;
            // Palm normal vector
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out pose))
                palmUpLeft = pose.Up;
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out pose))
                palmUpRight = pose.Up;
            // Palm forward vector
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out pose))
                palmFoLeft = pose.Forward;
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out pose))
                palmFoRight = pose.Forward;
        }

        private void StateTransition()
        {
            // 
            var newTap = false;
            //bool leftClicked = false, rightClicked = false;
            if (RightClickDetected())
            {
                if ((Time.realtimeSinceStartup - lastRightClickPressTime) > minChangeStateTimeThreshold)
                {
                    if (minRangeAdjustment || maxRangeAdjustment)
                    {
                        minRangeAdjustment = false;
                        maxRangeAdjustment = false;
                    }
                    else if (enableLeftTapForRange) adjustRadius = !adjustRadius;
                    else
                    {
                        holdRightTap = true;
                        newTap = true;
                    }
                }
                lastRightClickPressTime = Time.realtimeSinceStartup;
            }
            else if(!enableLeftTapForRange && Time.realtimeSinceStartup+lastRightClickPressTime > minChangeStateTimeThreshold)
            {
                holdRightTap = false;
            }
            //
            if (LeftClickDetected())
            {
                if ((Time.realtimeSinceStartup - lastLeftClickPressTime) > minChangeStateTimeThreshold)
                {
                    if (minRangeAdjustment || maxRangeAdjustment)
                    {
                        minRangeAdjustment = false;
                        maxRangeAdjustment = false;
                    }
                    else if (enableLeftTapForRange)
                    {
                        minRangeAdjustment = true;
                        maxRangeAdjustment = true;
                    }
                    else
                    {
                        holdLeftTap = true;
                        newTap = true;
                    }

                    if (minRangeAdjustment || maxRangeAdjustment)
                    {
                        timeChangingMat = 0;
                        matChangeDir = true;
                    }
                }
                lastLeftClickPressTime = Time.realtimeSinceStartup;
            }
            else if (!enableLeftTapForRange && Time.realtimeSinceStartup + lastLeftClickPressTime > minChangeStateTimeThreshold)
            {
                holdLeftTap = false;
            }
            //for material
            if (!minRangeAdjustment && !maxRangeAdjustment)
            {
                timeChangingMat = 0;
                MinMaxMaterialRange();
            }

            if (newTap) Debug.Log("New Tap Detected:" + "L:" + holdLeftTap + ", R:" + holdRightTap);
            // Two hands for enabling radius change, if left hand is not used for range selection
            if (!enableLeftTapForRange && newTap && holdLeftTap && holdRightTap )
            {
                adjustRadius = !adjustRadius;
            }
            // Show range labels
            //if (adjustRange || adjustRadius)
            //{
            //    minLabel.GetComponent<MeshRenderer>().enabled = true;
            //    maxLabel.GetComponent<MeshRenderer>().enabled = true;
            //}
            //else
            //{
            //    minLabel.GetComponent<MeshRenderer>().enabled = false;
            //    maxLabel.GetComponent<MeshRenderer>().enabled = false;
            //}
        }
        private bool RightClickDetected()
        {
            if (tTRight != null && iMRight != null && Vector3.Distance((Vector3)tTRight, (Vector3)iMRight) <= jointThreshold)
                return true;

            return false;
        }
        private bool LeftClickDetected()
        {
            if (tTLeft != null && iMLeft != null && Vector3.Distance((Vector3)tTLeft, (Vector3)iMLeft) <= jointThreshold)
                return true;

            return false;
        }
    }
}