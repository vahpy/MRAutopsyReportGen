using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloAutopsy.Record.Audio
{
    [ExecuteInEditMode]
    public class SpeechTextDialogue : MonoBehaviour
    {

        [SerializeField]
        private TMPro.TextMeshPro textField;
        [SerializeField]
        private TMPro.TextMeshPro statusField;
        [SerializeField]
        private Transform dialogueBox;

        private bool visible = false;
        private string text;

        private void Start()
        {
            this.text = "";
            this.visible = false;
        }

        public void NewResultString(string phrase)
        {
            if (phrase != null)
            {
                this.text += phrase;
                textField.text = this.text;
            }
        }
        public void NewHypothesisString(string guess)
        {
            if (guess != null)
            {
                textField.text = text + guess;
            }
        }
        public void NewErrorString(string error)
        {
            Debug.LogWarning("Warn " + error);
        }
        public void NewStatusMsg(string msg)
        {
            statusField.text = msg;
        }
        public void ToggleShowDialogue()
        {
            visible = !visible;
            if (visible)
            {
                dialogueBox.gameObject.SetActive(true);
            }
            else
            {
                dialogueBox.gameObject.SetActive(false);
            }
        }
    }
}