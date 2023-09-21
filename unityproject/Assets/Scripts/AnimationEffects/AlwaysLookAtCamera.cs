using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysLookAtCamera : MonoBehaviour
{
    void Update()
    {
        Vector3 pos = Camera.main.transform.position;
        if(this.transform.position - pos!=Vector3.zero) this.transform.rotation = Quaternion.LookRotation(this.transform.position-pos);
    }

    public static void AdaptRotation(Transform transform)
    {
        Vector3 pos = Camera.main.transform.position;
        if (transform.position - pos != Vector3.zero) transform.rotation = Quaternion.LookRotation(transform.position - pos);
    }
}
