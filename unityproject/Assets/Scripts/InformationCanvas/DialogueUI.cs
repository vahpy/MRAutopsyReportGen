using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HoloAuopsy
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private Camera camera;
        [SerializeField] private Transform autopsyTable;
        [SerializeField] private TMP_Text textLabel;

        [SerializeField][Range(0.1f,10)] private float activateDistance = 1.5f;
        [Multiline] [SerializeField] private string textToType;
        private bool isLabelShowing;

        private void Start()
        {
            isLabelShowing = false;
        }

        void Update()
        {
            float distance = Vector3.Distance(camera.transform.position, autopsyTable.position);
            if (!isLabelShowing && distance< activateDistance)
            {
                this.GetComponent<TypeWriterEffect>().Run(textToType, textLabel);
                isLabelShowing = true;
            }else if(!string.IsNullOrEmpty(textLabel.text) && distance >= activateDistance+0.5)
            {
                isLabelShowing = false;
                textLabel.text = "";
            }
        }
    }
}