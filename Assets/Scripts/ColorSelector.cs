using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSelector : MonoBehaviour
{
    public void SelectColor(string colorName)
    {

        Color selectedColor = Color.white;

        switch (colorName)
        {
            case "Red":
                selectedColor = Color.red;
                break;
            case "Blue":
                selectedColor = Color.blue;
                break;
            case "Green":
                selectedColor = Color.green;
                break;
            case "Yellow":
                selectedColor = Color.yellow;
                break;
            case "Magenta":
                selectedColor = new Color(0.7294f, 0.3333f, 0.8275f);
                break;
            default:
                selectedColor = Color.clear;
                break;
        }

        GameManager.Instance.SetSelectedColor(selectedColor);

    }
}