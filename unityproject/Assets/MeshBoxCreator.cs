using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy
{
    [ExecuteInEditMode]
    public class MeshBoxCreator : MonoBehaviour
    {
        [SerializeField, Range(1, 3)]
        private int meshLevel = 2;
        [SerializeField]
        private Material defaultMat = default;
        [SerializeField]
        private Mesh defaultMesh = default;

        private void OnEnable()
        {
            if (this.GetComponent<MeshFilter>() == null)
            {
                this.gameObject.AddComponent<MeshFilter>();
            }
            if (this.GetComponent<MeshFilter>().sharedMesh == null)
            {
                if (defaultMesh != null)
                {
                    this.GetComponent<MeshFilter>().sharedMesh = defaultMesh;
                }
                else
                {
                    this.GetComponent<MeshFilter>().sharedMesh = LevelOneBoxCreator();//BoxCreator(meshLevel);
                }
            }
            if (this.GetComponent<MeshRenderer>() == null)
            {
                this.gameObject.AddComponent<MeshRenderer>();
                if (defaultMat != null) this.GetComponent<MeshRenderer>().sharedMaterial = defaultMat;
            }
        }

        private static Mesh LevelOneBoxCreator()
        {
            Vector3[] vertices = new Vector3[] { new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
             new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f) };
            int[] tris = new int[] { 0, 1, 2,
            1, 3, 2,
            1, 5, 3,
            3, 5, 7,
            4, 7, 5,
            4, 6, 7,
            0, 6, 4,
            0, 2, 6,
            0, 4, 1,
            1, 4, 5,
            2, 3, 7,
            2, 7, 6 };
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }

        //public static Mesh BoxCreator(int level)
        //{
        //    switch (level)
        //    {
        //        case 1:
        //            return LevelOneBoxCreator();
        //        case 2:
        //            return LevelTwoBoxCreator();
        //        default:
        //            return LevelOneBoxCreator();
        //    }
        //}

        //private static Mesh LevelTwoBoxCreator()
        //{
        //    Vector3[] vertices = new Vector3[26];
        //    int offset = 0;
        //    for (int k = 0; k < 3; k++)
        //    {
        //        for (int j = 0; j < 3; j++)
        //        {
        //            for (int i = 0; i < 3; i++)
        //            {
        //                if (i == 1 && j == 1 & k == 1)
        //                {
        //                    offset -= 1;
        //                }
        //                else
        //                {
        //                    vertices[k * 9 + j * 3 + i + offset] = new Vector3(-0.5f + i * 0.5f, -0.5f + j * 0.5f, -0.5f + k * 0.5f);
        //                }
        //            }
        //        }
        //    }

        //    List<int> tris = new List<int>();
        //    tris.AddRange(LevelTwoGenerateFace1(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }));
        //    tris.AddRange(LevelTwoGenerateFace1(new int[] { 2, 11, 19, 5, 13, 22, 8, 16, 25 }));
        //    tris.AddRange(LevelTwoGenerateFace1(new int[] { 19, 18, 17, 22, 21, 20, 25, 24, 23 }));
        //    tris.AddRange(LevelTwoGenerateFace1(new int[] { 17, 9, 0, 20, 12, 3, 23, 14, 6 }));
        //    tris.AddRange(LevelTwoGenerateFace1(new int[] { 6, 7, 8, 14, 15, 16, 23, 24, 25 }));
        //    tris.AddRange(LevelTwoGenerateFace2(new int[] { 0, 1, 2, 9, 10, 11, 17, 18, 19 }));


        //    Mesh mesh = new Mesh();
        //    mesh.vertices = vertices;
        //    mesh.triangles = tris.ToArray();
        //    List<Vector2> uvs = new List<Vector2>();

        //    mesh.uv = uvs.ToArray();
        //    mesh.RecalculateBounds();
        //    mesh.RecalculateNormals();
        //    mesh.RecalculateTangents();
        //    mesh.RecalculateUVDistributionMetrics();
        //    return mesh;
        //}
        //private static List<Vector2> Level2UVMap(int[] vertices)
        //{
        //    Vector2[] uvs = new Vector2[vertices.Length];
        //    for(int i = 0; i < uvs.Length; i++)
        //    {
        //        uvs[i] = new Vector2(0,0);
        //    }
        //    //uvs[4] = new Vector2(1, 1);
        //    //uvs[10] = new Vector2(1, 1);
        //    //uvs[12] = new Vector2(1, 1);
        //    //uvs[13] = new Vector2(1, 1);
        //    //uvs[15] = new Vector2(1, 1);
        //    //uvs[21] = new Vector2(1, 1);

        //    uvs[1] = new Vector2(1, 1);
        //    uvs[3] = new Vector2(1, 1);
        //    uvs[5] = new Vector2(1, 1);
        //    uvs[7] = new Vector2(1, 1);
        //    uvs[9] = new Vector2(1, 1);
        //    uvs[11] = new Vector2(1, 1);
        //    uvs[18] = new Vector2(1, 1);
        //    uvs[20] = new Vector2(1, 1);
        //    uvs[24] = new Vector2(1, 1);
        //    uvs[22] = new Vector2(1, 1);
        //    uvs[16] = new Vector2(1, 1);
        //    uvs[14] = new Vector2(1, 1);
        //    uvs[24] = new Vector2(1, 1);




        //    return new List<Vector2>(uvs);
        //}
        //private static int[] LevelTwoGenerateFace1(int[] v)
        //{
        //    if (v.Length != 9) return null;
        //    int[] tris = new int[] {
        //    v[0],v[3], v[1],
        //    v[3], v[4],v[1],
        //    v[4], v[5],v[1],
        //    v[5], v[2],v[1],
        //    v[6],v[7],v[3],
        //    v[7],v[4],v[3],
        //    v[7],v[5],v[4],
        //    v[7],v[8],v[5]
        //};
        //    return tris;
        //}
        //private static int[] LevelTwoGenerateFace2(int[] v)
        //{
        //    if (v.Length != 9) return null;
        //    int[] tris = new int[] {
        //    v[0],v[1], v[3],
        //    v[1], v[4],v[3],
        //    v[1], v[2],v[5],
        //    v[1], v[5],v[4],
        //    v[3],v[7],v[6],
        //    v[3],v[4],v[7],
        //    v[7],v[5],v[8],
        //    v[7],v[4],v[5]
        //};
        //    return tris;
        //}
    }

}