using UnityEngine;

namespace HoloAutopsy
{
    public class SceneLocator : MonoBehaviour
    {
        [SerializeField]
        private Transform autopsyRoom = default;

        void Update()
        {
            if (!transform.hasChanged) return;
            transform.hasChanged = false;
            autopsyRoom.position = transform.position;
            autopsyRoom.rotation = transform.rotation;
        }
    }
}