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
                selectedColor = Color.magenta;
                break;
        }

        GameManager.Instance.SetSelectedColor(selectedColor);

    }
}