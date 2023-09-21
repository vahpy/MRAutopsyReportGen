using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomOnSlider : MonoBehaviour
{
    [SerializeField] private bool enable = false;
    [SerializeField] private GameObject sliderLine = default;
    [SerializeField] private int stop = 44;


    private Vector3[] vertices = null;
    private Vector3[] newVertices = null;
    private Mesh mesh = null;
    private void Start()
    {
        // Get instantiated mesh
        mesh = sliderLine.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        newVertices = new Vector3[vertices.Length+84];
    }
    void Update()
    {

    }

    public void updateSliderValue(SliderEventData data)
    {
        if (!enable || data == null || data.Slider == null || data.Slider.ThumbRoot == null) return;
        changeMesh(data.Slider.ThumbRoot.transform.position.x, 0.3f);
    }
    void changeMesh(float currentPos, float magnitude)
    {
        if (vertices == null || newVertices == null) return;

        //const float distance = 0.2f;
        float offset = 0;
        bool extruded = false;
        int p = 0,i=0;
        float step = Mathf.Abs(vertices[6].x - vertices[0].x)/85;
        while (p < vertices.Length)
        {
            //Debug.Log("T.x = "+ temp.x +", C.x =" + currentPos);
            if (!extruded && p==stop)//Mathf.Abs(vertices[p].x - currentPos) < distance)
            {
                extruded = true;
                i = -42;
                while (i < 42)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        newVertices[p + i] = new Vector3(currentPos + i * step, vertices[p+j].y, vertices[p+j].z + offset);
                        i++;
                    }
                }
            }
            else
            {
                newVertices[p+i] = vertices[p];
            }
            p++;
        }
        mesh.vertices = newVertices;
        mesh.RecalculateNormals();
    }
}

//offset = 1 - Mathf.Pow(temp.x - currentPos, 2);
//if (offset != 0) offset = magnitude / offset;
//else offset = magnitude;