using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlinkColorSelected : MonoBehaviour
{
    public TextMeshProUGUI caretText; // Objeto de texto que contiene "^"
    public float blinkSpeed = 0.5f;   // Velocidad del parpadeo

    private bool isBlinking = false;

    private void Awake()
    {
        caretText.enabled = false; // Asegurar que el "^" esté desactivado al inicio
    }

    public void StartBlinking()
    {
        if (!isBlinking)
        {
            isBlinking = true;
            caretText.enabled = true; // Hacer visible el "^"
            StartCoroutine(BlinkCoroutine());
        }
    }

    public void StopBlinking()
    {
        isBlinking = false;
        StopAllCoroutines();
        caretText.enabled = false; // Ocultar el "^"
    }

    private IEnumerator BlinkCoroutine()
    {
        while (isBlinking)
        {
            caretText.enabled = !caretText.enabled;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
}

