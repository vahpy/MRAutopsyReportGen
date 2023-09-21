using System;
using UnityEngine;
using UnityVolumeRendering;

[ExecuteInEditMode]
public class UpgradeVolumeRenderingPackage : MonoBehaviour
{
    [SerializeField]
    private bool interpolate = false;
    private bool _lastInterpolate = false;
    [SerializeField]
    private bool generated = false;
    [SerializeField]
    private UnityVolumeRendering.VolumeRenderedObject volObj;
    [SerializeField]
    private MeshRenderer volRenderer;
    private Material material;
    private Texture3D dataTex;
    private Texture3D gradTex;
    void OnEnable()
    {
        material = volRenderer.sharedMaterial;
    }

    void Update()
    {
        if (_lastInterpolate != interpolate)
        {
            _lastInterpolate = interpolate;
            Regenerate();
        }
    }

    private void Regenerate()
    {

        if (generated) return;
        generated = true;
        Debug.Log(volObj.dataset.data.Length);
        Debug.Log(volObj.dataset.GetDataTexture().width);
        Debug.Log(volObj.dataset.GetGradientTexture().width);
        //RegenerateDataArray();
    }

    private void RegenerateDataArray()
    {

        int dimX = dataTex.width, dimY = dataTex.height, dimZ = dataTex.depth;
        float[] data = new float[dimX * dimY * dimZ];
        Debug.Log(dataTex.width + "," + dataTex.height + "," + dataTex.depth);
        try
        {
            TextureFormat texformat = SystemInfo.SupportsTextureFormat(TextureFormat.RHalf) ? TextureFormat.RHalf : TextureFormat.RFloat;
            if (texformat == TextureFormat.RHalf)
            {
                //data = dataTex.GetPixelData<float>(0).ToArray();

                for (int x = 0; x < dimX; x++)
                {
                    for (int y = 0; y < dimY; y++)
                    {
                        for (int z = 0; z < dimZ; z++)
                        {
                            int iData = x + y * dimX + z * (dimX * dimY);
                            data[iData] = dataTex.GetPixel(x, y, z).r;
                        }
                    }
                }
            }
            else
            {
                data = dataTex.GetPixelData<float>(0).ToArray();
            }
            Debug.Log("data length : " + data.Length);
            if (data.Length == 62128128)
            {
                volObj.dataset.data = data;
                Debug.Log("Data Set");
            }
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Regenerating data failed!");
        }

    }
}
