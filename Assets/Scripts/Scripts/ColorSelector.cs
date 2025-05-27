using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSelector : MonoBehaviour
{
    private static readonly Dictionary<string, Color> ColorMap = new Dictionary<string, Color>
    {
        //Colores del bloque 1 (base):
        { "Red", Color.red },
        { "Blue", Color.blue },
        { "Green", Color.green },
        { "Yellow", Color.yellow },
        { "Magenta", Color.magenta },

        //Colores del bloque 1 de niveles (1-16):
        { "TuftsBlue", new Color(0.2902f, 0.5647f, 0.8863f) },
        { "Saffron", new Color(0.9608f, 0.7725f, 0.0941f) },
        { "IndianRed", new Color(0.8510f, 0.3255f, 0.3098f) },
        { "Emerald", new Color(0.4253f, 0.7490f, 0.5176f) },
        { "PurpleMountainMajesty", new Color(0.6431f, 0.4275f, 0.6784f) },

        //Colores del bloque 2 de niveles (17-32):
        { "CyberGrape", new Color(0.3176f, 0.2275f, 0.4078f) },
        { "GreenLizard", new Color(0.7451f, 0.9804f, 0.3098f) },
        { "MaximumBlue", new Color(0.3529f, 0.6549f, 0.7255f) },
        { "FashionFuchsia", new Color(0.9098f, 0.2000f, 0.6000f) },
        { "AmaranthRed", new Color(0.8235f, 0.1529f, 0.1882f) },

        //Colores del bloque 3 de niveles (33-48):
        { "GreenNCS", new Color(0.1333f, 0.6353f, 0.4392f) },
        { "DutchWhite", new Color(0.9255f, 0.8863f, 0.7765f) },
        { "Amethyst", new Color(0.6000f, 0.4000f, 0.8000f) },
        { "YellowOrangeColorWheel", new Color(1.0000f, 0.5686f, 0.0000f) },
        { "FrenchSkyBlue", new Color(0.3961f, 0.6863f, 1.0000f) },

        //Colores del bloque 4 de niveles (49-64):
        { "BlackOlive", new Color(0.2549f, 0.2745f, 0.2392f) },
        { "VividBurgundy", new Color(0.6392f, 0.0431f, 0.2157f) },
        { "MediumPurple", new Color(0.6157f, 0.5529f, 0.9451f) },
        { "MaizeCrayola", new Color(0.9529f, 0.7882f, 0.3843f) },
        { "Champagne", new Color(0.9176f, 0.8471f, 0.7451f) }
    };

    public void SelectColor(string colorName)
    {
        Color selectedColor = GetColorByName(colorName);
        ApplySelectedColor(selectedColor);
    }

    private Color GetColorByName(string colorName)
    {
        return ColorMap.TryGetValue(colorName, out Color color) ? color : GetDefaultColor();
    }

    private Color GetDefaultColor()
    {
        return Color.clear;
    }

    private void ApplySelectedColor(Color color)
    {
        GameManager.Instance.SetSelectedColor(color);
    }
}