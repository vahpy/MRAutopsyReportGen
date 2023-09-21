using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourMapPicker : MonoBehaviour
{
    [SerializeField]
    private Transform buttonCollection = default;

    List<string> colourMapNames;

    private void Awake()
    {
        colourMapNames = new List<string>();
    }

    #region PUBLIC_API
    public void ColourMapPicked(int btnId)
    {

    }


    public void Btn0()
    {
        ColourMapPicked(0);
    }
    public void Btn1()
    {
        ColourMapPicked(1);
    }
    public void Btn2()
    {
        ColourMapPicked(2);
    }
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}
    //public void Btn0()
    //{
    //    ColourMapPicked(0);
    //}

    #endregion
}
