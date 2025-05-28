using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceResolution : MonoBehaviour
{
    void Start()
    {
        int width = 540;
        int height = 960;
        bool fullscreen = false;

        Screen.SetResolution(width, height, fullscreen);
    }
}
