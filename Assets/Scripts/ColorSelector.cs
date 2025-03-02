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
            // Paleta de prueva num1:
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
            // Paleta de prueva num2:
            case "RedPigment":
                selectedColor = new Color(0.9294f, 0.1098f, 0.1412f);
                break;
            case "Keppel":
                selectedColor = new Color(0.2314f, 0.6627f, 0.6118f);
                break;
            case "BdazzledBlue":
                selectedColor = new Color(0.1373f, 0.3412f, 0.5373f);
                break;
            case "SafetyYellow":
                selectedColor = new Color(0.9451f, 0.8275f, 0.0078f);
                break;
            // Paleta de prueva num3:
            case "PortlandOrange":
                selectedColor = new Color(0.9569f, 0.3765f, 0.2039f);
                break;
            case "SteelBlue":
                selectedColor = new Color(0.3569f, 0.5216f, 0.6667f);
                break;
            case "QueenPink":
                selectedColor = new Color(0.9255f, 0.7961f, 0.8510f);
                break;
            case "LaurelGreen":
                selectedColor = new Color(0.6275f, 0.6863f, 0.5176f);
                break;
            default:
                selectedColor = Color.clear;
                break;
        }

        GameManager.Instance.SetSelectedColor(selectedColor);

    }
}