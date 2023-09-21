using UnityEngine;

namespace HoloAutopsy
{
    public class EWManager : MonoBehaviour
    {
        public static EWManager Instance { get; private set; }

        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private AudioClip errorClip;
        [SerializeField]
        private AudioClip warningClip;
        [SerializeField]
        private AudioClip confirmClip;


        [SerializeField, Range(0, 1)]
        private float volume = 0.5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// If there is no message, just play the error sound
        /// </summary>
        /// <param name="msg"></param>
        public static void Error(string msg)
        {
            Instance.audioSource.PlayOneShot(Instance.errorClip, Instance.volume);
            if (!string.IsNullOrEmpty(msg)) Debug.LogError(msg);
        }

        /// <summary>
        /// If there is no message, just play the warning sound
        /// </summary>
        /// <param name="msg"></param>
        public static void Warning(string msg)
        {
            Instance.audioSource.PlayOneShot(Instance.warningClip, Instance.volume);
            if (!string.IsNullOrEmpty(msg)) Debug.LogWarning(msg);
        }

        /// <summary>
        /// If there is no message, just play the confirmation sound
        /// </summary>
        /// <param name="msg"></param>
        public static void Confirm(string msg = null)
        {
            Instance.audioSource.PlayOneShot(Instance.confirmClip, Instance.volume);
            if (!string.IsNullOrEmpty(msg)) Debug.Log(msg);
        }

        public static void Log(string msg)
        {
            if (!string.IsNullOrEmpty(msg)) Debug.Log(msg);
        }
    }
}