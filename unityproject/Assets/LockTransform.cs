using UnityEngine;

namespace HoloAuopsy
{
    public class LockTransform : MonoBehaviour
    {
        [SerializeField] private bool lockMovement = false;
        [SerializeField] private bool lockRotation = false;
        [SerializeField] private bool lockScale = false;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private Vector3 initialLocalScale;
        void Start()
        {
            initialLocalPosition = this.transform.localPosition;
            initialLocalRotation = this.transform.localRotation;
            initialLocalScale = this.transform.localScale;
            this.transform.hasChanged = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (this.transform.hasChanged)
            {
                if (lockMovement) this.transform.localPosition = initialLocalPosition;
                if (lockRotation) this.transform.localRotation = initialLocalRotation;
                if (lockScale) this.transform.localScale = initialLocalScale;
                this.transform.hasChanged = false;
            }
        }
    }
}