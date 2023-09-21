using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using UnityEngine;


public class BrowserUtils
{
    public static byte[] ImageFileToByteArray(string imageFilePath)
    {
        return File.ReadAllBytes(imageFilePath);
    }
}

