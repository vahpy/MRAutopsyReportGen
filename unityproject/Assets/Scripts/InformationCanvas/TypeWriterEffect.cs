using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HoloAuopsy
{
    public class TypeWriterEffect : MonoBehaviour
    {
        [SerializeField] private float typewriterSpeed = 50f;

        public void Run(string textToType, TMP_Text textLabel)
        {
            StartCoroutine(TypeText(textToType, textLabel));
        }
        private IEnumerator TypeText(string textToType, TMP_Text textLabel)
        {
            //yield return new WaitForSeconds(2); //initial delay
            float t = 0;
            int charIndex = 0;

            while (charIndex < textToType.Length)
            {
                t += Time.deltaTime * typewriterSpeed;
                charIndex = Mathf.FloorToInt(t);
                charIndex = Mathf.Clamp(charIndex, 0, textToType.Length);
                textLabel.text = textToType.Substring(0, charIndex);
                yield return null;
            }
            textLabel.text = textToType;
        }
    }
}