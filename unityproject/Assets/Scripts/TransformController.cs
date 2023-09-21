using UnityEngine;
using UnityEngine.Events;

namespace HoloAuopsy
{
    public class TransformController : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent events;
        void Update()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                events.Invoke();
            }
        }
    }

    public class TransformEvent : UnityEvent
    {
        public GameObject origin;

    }
}