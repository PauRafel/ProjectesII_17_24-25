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


            // Colores de niveles de aprendizaje (tutorial - nivel8):
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

            // Paleta: TEMÁTICA ESTACIONES --> OTOÑO
            case "AntiqueBronze": // DarkGreen
                selectedColor = new Color(0.3451f, 0.3176f, 0.1373f);
                break;
            case "Bronze": // Orange
                selectedColor = new Color(0.8353f, 0.5373f, 0.2118f);
                break;
            case "Rosewood": // Red
                selectedColor = new Color(0.3529f, 0.0667f, 0.0471f);
                break;
            case "LightFrenchBeige": // Beige
                selectedColor = new Color(0.7608f, 0.6588f, 0.4902f);
                break;

            // Paleta: TEMÁTICA ESTACIONES --> INVIERNO
            case "QuickSilver": // Grey
                selectedColor = new Color(0.6275f, 0.6275f, 0.6275f);
                break;
            case "PeriwinkleCrayola": // LightBlue
                selectedColor = new Color(0.7725f, 0.8431f, 0.9686f);
                break;
            case "GreenBlue": // DarkBlue
                selectedColor = new Color(0.1882f, 0.4196f, 0.6745f);
                break;
            case "Beaver": // Brown
                selectedColor = new Color(0.6392f, 0.5059f, 0.4078f);
                break;

            // Paleta: TEMÁTICA ESTACIONES --> PRIMAVERA
            case "YellowCrayola": // Yellow
                selectedColor = new Color(0.9882f, 0.9059f, 0.4902f);
                break;
            case "UranianBlue": // Blue
                selectedColor = new Color(0.7059f, 1.0f, 1.0f);
                break;
            case "CherryBlossomPink": // Pink
                selectedColor = new Color(1.0f, 0.7176f, 0.7725f);
                break;
            case "MintGreen": // Green
                selectedColor = new Color(0.5961f, 0.9843f, 0.5961f);
                break;
            case "PurpleMountainMajesty": // Purple
                selectedColor = new Color(0.6431f, 0.4275f, 0.6784f);
                break;

            // Paleta: TEMÁTICA ESTACIONES --> VERANO
            case "SkyBlue":
                selectedColor = new Color(0.5294f, 0.8078f, 0.9216f);
                break;
            case "CedarChest":
                selectedColor = new Color(0.8510f, 0.3529f, 0.3137f);
                break;
            case "Melon":
                selectedColor = new Color(1.0f, 0.7098f, 0.6549f);
                break;
            case "Sunray":
                selectedColor = new Color(0.9294f, 0.6824f, 0.2863f);
                break;


            // Paleta de prueba num1:
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

            // Paleta de prueba num2:
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

            // Paleta de prueba num3:
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
