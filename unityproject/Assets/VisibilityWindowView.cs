using UnityEngine;
using UnityVolumeRendering;

public class VisibilityWindowView : MonoBehaviour
{
    [SerializeField] private Transform chevronStart = default;
    [SerializeField] private Transform chevronEnd = default;
    [SerializeField] private VolumeRenderedObject volumeObj = default;
    private const float OFFSET = 0.5f;
    private Quaternion defaultRotation = new Quaternion(0, 0, 0, 0);
    void Start()
    {
        chevronStart.hasChanged = false;
        chevronEnd.hasChanged = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (chevronStart.hasChanged || chevronEnd.hasChanged)
        {
            Vector3 temp = new Vector3(0,0,0);
            temp.x = Mathf.Clamp(chevronStart.localPosition.x, -OFFSET, +OFFSET);
            chevronStart.localPosition = temp;
            temp.x = Mathf.Clamp(chevronEnd.localPosition.x, -OFFSET, +OFFSET);
            chevronEnd.localPosition = temp;

            chevronStart.localRotation = defaultRotation;
            chevronEnd.localRotation = defaultRotation;

            volumeObj.SetVisibilityWindow(chevronStart.localPosition.x + OFFSET, chevronEnd.localPosition.x + OFFSET);
            chevronStart.hasChanged = false;
            chevronEnd.hasChanged = false;
        }
    }
}
