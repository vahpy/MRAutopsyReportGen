using UnityEngine;

namespace HoloAuopsy
{
    public class TrackedObject : MonoBehaviour
    {
        internal bool isLive { private set; get; }
        public Vector3 lastValidPosition;
        void Start()
        {
            isLive = false;
            lastValidPosition = new Vector3(0, 0, 0);
        }

        public void SetLivePosition(Vector3 position)
        {
            lastValidPosition = position;
            isLive = true;
        }
    }
}