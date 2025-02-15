using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarDisplay : MonoBehaviour
{
    public Image[] starImages; // Asigna los 3 objetos de imagen en el inspector
    public Sprite starFull; // Sprite de estrella llena
    public Sprite starEmpty; // Sprite de estrella vacía

    public void SetStars(int starCount)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            if (i < starCount)
                starImages[i].sprite = starFull;
            else
                starImages[i].sprite = starEmpty;
        }
    }
}
