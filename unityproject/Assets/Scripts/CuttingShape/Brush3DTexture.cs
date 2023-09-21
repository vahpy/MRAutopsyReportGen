using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;

namespace HoloAutopsy.CuttingShape
{
    [ExecuteInEditMode]
    public class Brush3DTexture : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader compute;

        private UnityVolumeRendering.VolumeRenderedObject volObjParent;
        [SerializeField]
        private Transform volObj;

        private RenderTexture maskTex;
        [SerializeField]
        private int radius = 14;
        [SerializeField]
        private float softAreaThreshold = 10.0f;
        [SerializeField]
        private bool eraseOrDraw = true;
        [SerializeField]
        private bool doAction = false;
        private Texture3D growingBrushTex = default;

        //Types of Brush
        enum BrushType { Hard, Windowed, Growing };
        [SerializeField]
        private BrushType brushType = BrushType.Hard;

        // logic control fields
        private Vector3 _lastBrushLocPos;
        private GameObject firstChild;

        //Kernel IDs
        //private int kernelID;
        private int Initialize_kernelID;
        private int HardBrush_kernelID;
        private int GrowingBrush_kernelID; // keep radius <=14 to have a real-time frame rate
        private int WindowedBrush_kernelID;

        private void OnEnable()
        {
            _lastBrushLocPos = transform.localPosition - Vector3.up;
            firstChild = this.transform.GetChild(0).gameObject;
            volObjParent = volObj.GetComponentInParent<UnityVolumeRendering.VolumeRenderedObject>();
        }


        // Update is called once per frame
        void Update()
        {
            if (!volObjParent.GetEraserEnabled())
            {
                if (firstChild.GetComponent<MeshRenderer>().enabled) firstChild.GetComponent<MeshRenderer>().enabled = false;
                doAction = false;
                return;
            }
            else
            {
                if (!firstChild.GetComponent<MeshRenderer>().enabled)
                {
                    doAction = false;
                    firstChild.GetComponent<MeshRenderer>().enabled = true;
                }
            }
            AssignMaskTex(volObjParent.GetMaskTexture());
            if (_lastBrushLocPos == transform.localPosition || this.maskTex == null) return;
            _lastBrushLocPos = transform.localPosition;


            Vector3 dataDim = new Vector3(volObjParent.dataset.dimX, volObjParent.dataset.dimY, volObjParent.dataset.dimZ);
            Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);
            var texPos = volObj.worldToLocalMatrix.MultiplyPoint(transform.position);
            texPos += offset;
            texPos.Scale(dataDim);

            compute.SetVector("center", texPos);
            compute.SetFloat("radius", radius);
            compute.SetBool("erase_draw", eraseOrDraw);
            compute.SetFloat("diagonal", radius * 2);
            //Add visibility window
            var minVis = volObjParent.GetVisibilityWindow().x;
            var maxVis = volObjParent.GetVisibilityWindow().y;
            if (minVis > maxVis)
            {
                var temp = minVis;
                minVis = maxVis;
                maxVis = temp;
            }
            compute.SetFloat("minVisible", minVis);
            compute.SetFloat("maxVisible", maxVis);
            compute.SetVector("minPoint", texPos - new Vector3(radius, radius, radius));
            if (doAction)
            {
                compute.SetBool("onlySelect", false);
            }
            else
            {
                compute.SetBool("onlySelect", true);
            }

            int rigidRadius = radius / 5 + (radius % 5 != 0 ? 1 : 0);
            Vector3Int texPosInt = new Vector3Int((int)texPos.x, (int)texPos.y, (int)texPos.z);
            if (brushType == BrushType.Hard)
            {
                compute.Dispatch(HardBrush_kernelID, volObjParent.dataset.dimX / 8, volObjParent.dataset.dimY / 8, volObjParent.dataset.dimZ);
            }
            else if (brushType == BrushType.Windowed)
            {
                compute.Dispatch(WindowedBrush_kernelID, volObjParent.dataset.dimX / 8, volObjParent.dataset.dimY / 8, volObjParent.dataset.dimZ);
            }
            else if (brushType == BrushType.Growing)
            {
                if (this.growingBrushTex != null)
                {
                    DestroyImmediate(growingBrushTex);
                    this.growingBrushTex = null;
                }
                this.growingBrushTex = GetMagicSelectedVoxels(texPosInt, rigidRadius, radius);
                compute.SetTexture(GrowingBrush_kernelID, "voxels", this.growingBrushTex);
                compute.Dispatch(GrowingBrush_kernelID, volObjParent.dataset.dimX / 8, volObjParent.dataset.dimY / 8, volObjParent.dataset.dimZ);
            }
        }

        #region PUBLIC_API
        public void GrabStart()
        {
            doAction = true;
        }
        public void GrabEnd()
        {
            doAction = false;
        }
        public void EraseEnable()
        {
            eraseOrDraw = true;
        }
        public void DrawEnable()
        {
            eraseOrDraw = false;
        }
        #endregion

        private void AssignMaskTex(RenderTexture maskTex)
        {
            if (this.maskTex == maskTex) return;
            this.maskTex = maskTex;
            if (this.maskTex != null)
            {
                Initialize_kernelID = compute.FindKernel("CSInitialize");
                GrowingBrush_kernelID = compute.FindKernel("CSGrowingBrush");
                WindowedBrush_kernelID = compute.FindKernel("CSWindowedBrush");
                HardBrush_kernelID = compute.FindKernel("CSHardBrush");
                compute.SetTexture(Initialize_kernelID, "Result", this.maskTex);
                compute.SetTexture(HardBrush_kernelID, "Result", this.maskTex);
                compute.SetTexture(WindowedBrush_kernelID, "Result", this.maskTex);
                compute.SetTexture(GrowingBrush_kernelID, "Result", this.maskTex);
                compute.SetTexture(Initialize_kernelID, "DataTex", volObjParent.dataset.GetDataTexture());
                compute.SetTexture(HardBrush_kernelID, "DataTex", volObjParent.dataset.GetDataTexture());
                compute.SetTexture(WindowedBrush_kernelID, "DataTex", volObjParent.dataset.GetDataTexture());
                compute.SetTexture(GrowingBrush_kernelID, "DataTex", volObjParent.dataset.GetDataTexture());
                InitializeComputeShader();
                //Debug.Log(volObjParent.dataset.GetDataTexture().width + "," + volObjParent.dataset.GetDataTexture().height + "," + volObjParent.dataset.GetDataTexture().depth);
                //Debug.Log(this.maskTex.width + "," + this.maskTex.height + "," + this.maskTex.volumeDepth);
            }
        }

        private void InitializeComputeShader()
        {
            if (!volObjParent.IsMaskInitialized())
            {
                compute.Dispatch(Initialize_kernelID, volObjParent.dataset.dimX / 8, volObjParent.dataset.dimY / 8, volObjParent.dataset.dimZ);
                volObjParent.SetMaskInitialized(true);
                Debug.Log("Mask Initialized.");
            }
            else
            {
                Debug.Log("Mask already Initialized.");
            }
        }

        /// <summary>
        /// Returns a Texture3D\<int\> where negative values means not selected voxels
        /// </summary>
        /// <param name="radius">radius counted as the number of voxels</param>
        /// <returns></returns>
        public Texture3D GetMagicSelectedVoxels(Vector3Int center, int rigidRadius, int softRadius)
        {
            int rigidDim = 2 * rigidRadius;
            int softDim = 2 * softRadius;
            int[,,] states = new int[softDim, softDim, softDim];
            float sum = 0;
            int count = 0;
            float floatRigidRadius = rigidRadius;
            float floatSoftRadius = softRadius;


            Texture3D tex = new Texture3D(softDim, softDim, softDim, TextureFormat.RFloat, false);
            List<Vector3Int> tempList = new List<Vector3Int>();

            int rigTexPt = softRadius - rigidRadius;

            for (int i = 0; i < rigidDim; i++)
            {
                for (int j = 0; j < rigidDim; j++)
                {
                    for (int k = 0; k < rigidDim; k++)
                    {
                        try
                        {
                            var voxel = new Vector3Int(i - rigidRadius, j - rigidRadius, k - rigidRadius);
                            if (voxel.magnitude <= floatRigidRadius)
                            {
                                var value = volObjParent.dataset.GetData(center.x + i - rigidRadius, center.y + j - rigidRadius, center.z + k - rigidRadius);
                                states[i + rigTexPt, j + rigTexPt, k + rigTexPt] = 1;
                                count++;
                                sum += value;
                                tempList.Add(new Vector3Int(i + rigTexPt, j + rigTexPt, k + rigTexPt));
                                tex.SetPixel(i + rigTexPt, j + rigTexPt, k + rigTexPt, new Color(1.0f, 1.0f, 1.0f, 1.0f));
                            }
                        }
                        catch { }
                    }
                }
            }
            float avg = sum / count;
            float minRange = avg - softAreaThreshold;
            float maxRange = avg + softAreaThreshold;

            //Debug.Log("Voxels in rigid=" + count + ". Min=" + minRange + ", Max=" + maxRange);
            count = 0;
            int catched = 0;
            int notValid = 0;
            for (int i = 0; i < softDim; i++)
            {
                for (int j = 0; j < softDim; j++)
                {
                    for (int k = 0; k < softDim; k++)
                    {
                        if (states[i, j, k] == 1) continue;

                        var voxel = new Vector3Int(i - softRadius, j - softRadius, k - softRadius);
                        try
                        {
                            if (voxel.magnitude <= floatSoftRadius)
                            {
                                var tempPos = center + voxel;
                                var value = volObjParent.dataset.GetData(tempPos.x, tempPos.y, tempPos.z);
                                if (value >= minRange && value <= maxRange)
                                {
                                    //Debug.Log("Val=" + value);
                                    states[i, j, k] = 0;
                                    count++;
                                }
                                else
                                {
                                    states[i, j, k] = -1;
                                    notValid++;
                                }
                            }
                            else
                            {
                                states[i, j, k] = -1;
                                notValid++;
                            }
                        }
                        catch
                        {
                            states[i, j, k] = -1;
                            catched++;
                        }
                        finally
                        {
                            tex.SetPixel(i, j, k, new Color(0.0f, 0.0f, 0.0f, 0.0f));
                        }
                    }
                }
            }
            var x = tempList.Count;
            //int zeros = 0;
            for (int i = 0; i < tempList.Count; i++)
            {
                var temp = tempList[i];

                ColorValidVoxel(new Vector3Int(temp.x - 1, temp.y, temp.z), softRadius, states, tempList, tex);
                ColorValidVoxel(new Vector3Int(temp.x, temp.y - 1, temp.z), softRadius, states, tempList, tex);
                ColorValidVoxel(new Vector3Int(temp.x, temp.y, temp.z - 1), softRadius, states, tempList, tex);
                ColorValidVoxel(new Vector3Int(temp.x + 1, temp.y, temp.z), softRadius, states, tempList, tex);
                ColorValidVoxel(new Vector3Int(temp.x, temp.y + 1, temp.z), softRadius, states, tempList, tex);
                ColorValidVoxel(new Vector3Int(temp.x, temp.y, temp.z + 1), softRadius, states, tempList, tex);
            }
            tex.Apply();
            //Debug.Log("not valid=" + notValid + ", catched exception:" + catched /*+ ", zeros=" + zeros*/);
            //Debug.Log("Count: " + x + "=>" + tempList.Count + "< maximum possible voxels:" + (count + x));
            return tex;
        }

        private static bool ColorValidVoxel(Vector3Int texPos, int offset, int[,,] states, List<Vector3Int> validVoxelList, Texture3D tex)
        {
            try
            {
                if (states[texPos.x, texPos.y, texPos.z] == 0)
                {
                    states[texPos.x, texPos.y, texPos.z] = 1;
                    validVoxelList.Add(texPos);
                    tex.SetPixel(texPos.x, texPos.y, texPos.z, new Color(1.0f, 1.0f, 1.0f, 1.0f));
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}