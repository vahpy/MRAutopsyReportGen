using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RotatingObject : MonoBehaviour
{
    [SerializeField]
    private Transform pivot = default;
    private bool inUse = false;
    private float rotationSpeed = 2f;
    void Update()
    {
        if(inUse) return;
        transform.Rotate(pivot.forward, rotationSpeed);
        transform.position = pivot.position + new Vector3(0,0.1f,0);
    }

    public void SetUse(bool use)
    {
        inUse = use;
    }
}
