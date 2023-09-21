using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.Events;

public class MenuActions : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Assign DialogSmall_192x96.prefab")]
    private GameObject dialogPrefabSmall;

    [Header("Events")]
    [SerializeField] private UnityEvent events;

    /// <summary>
    /// Small Dialog example prefab to display
    /// </summary>
    public GameObject DialogPrefabSmall
    {
        get => dialogPrefabSmall;
        set => dialogPrefabSmall = value;
    }



    void Start()
    {
        //Dialog dialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.Yes | DialogButtonType.No, "Collaboration Settings", "Do you want to load a collaborative environment? Choose No for working alone.", true);
    }
}
