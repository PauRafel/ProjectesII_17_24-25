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
            // Paleta de prueva:
            case "RaisinBlack":
                selectedColor = new Color(0.1176f, 0.1176f, 0.1412f);
                break;
            case "DarkRed":
                selectedColor = new Color(0.5725f, 0.0784f, 0.0471f);
                break;
            case "FloralWhite":
                selectedColor = new Color(1.0f, 0.9725f, 0.9412f);
                break;
            case "DeepChampagne":
                selectedColor = new Color(1.0f, 0.8118f, 0.6f);
                break;
            default:
                selectedColor = Color.clear;
                break;
        }

        GameManager.Instance.SetSelectedColor(selectedColor);

    }
}