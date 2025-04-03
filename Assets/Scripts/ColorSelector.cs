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
            // Colores de prueba de niveles:
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

            // Colores de niveles de aprendizaje:
            case "TuftsBlue": // Blue
                selectedColor = new Color(0.2902f, 0.5647f, 0.8863f);
                break;
            case "Saffron": // Yellow
                selectedColor = new Color(0.9608f, 0.7725f, 0.0941f);
                break;
            case "IndianRed": // Red
                selectedColor = new Color(0.8510f, 0.3255f, 0.3098f);
                break;
            case "Emerald": // Green
                selectedColor = new Color(0.4253f, 0.7490f, 0.5176f);
                break;
            case "PurpleMountainMajesty": // Purple
                selectedColor = new Color(0.6431f, 0.4275f, 0.6784f);
                break;

            // Paleta de Colores Neón:
            case "CyberGrape": // Purple
                selectedColor = new Color(0.3176f, 0.2275f, 0.4078f);
                break;
            case "GreenLizard": // Green
                selectedColor = new Color(0.7451f, 0.9804f, 0.3098f);
                break;
            case "MaximumBlue": // Blue
                selectedColor = new Color(0.3529f, 0.6549f, 0.7255f);
                break;
            case "FashionFuchsia": // Fuchsia
                selectedColor = new Color(0.9098f, 0.2000f, 0.6000f);
                break;
            case "AmaranthRed": // Red
                selectedColor = new Color(0.8235f, 0.1529f, 0.1882f);
                break;

            // Paleta de Colores Tecnología/Futuro:
            case "GreenNCS": // Green
                selectedColor = new Color(0.1333f, 0.6353f, 0.4392f);
                break;
            case "DutchWhite": // White
                selectedColor = new Color(0.9255f, 0.8863f, 0.7765f);
                break;
            case "Amethyst": // Purple
                selectedColor = new Color(0.6000f, 0.4000f, 0.8000f);
                break;
            case "YellowOrangeColorWheel": // Orange
                selectedColor = new Color(1.0000f, 0.5686f, 0.0000f);
                break;
            case "FrenchSkyBlue": // Blue
                selectedColor = new Color(0.3961f, 0.6863f, 1.0000f);
                break;

            default:
                selectedColor = Color.clear;
                break;
        }

        GameManager.Instance.SetSelectedColor(selectedColor);

    }
}
