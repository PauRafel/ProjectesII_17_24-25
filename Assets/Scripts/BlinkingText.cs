using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class BlinkingText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;  // TextMeshPro asignado en el Inspector
    public float blinkDuration = 5f;       // Tiempo total de parpadeo
    public float blinkSpeed = 0.5f;        // Velocidad de parpadeo
    
    private string originalText;           // Texto original completo
    private string blinkingWord;           // Palabra con su color original
    private string hiddenWord;             // Palabra oculta con transparencia
    private string baseText;               // Texto sin la palabra que parpadea

    private void Start()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        if (textComponent != null)
        {
            originalText = textComponent.text;
            ExtractBlinkingWord();
            StartCoroutine(BlinkCoroutine());
        }
    }

    private void ExtractBlinkingWord()
    {
        // Expresión regular para encontrar una palabra con color: <#XXXXXX>WORD</color>
        Match match = Regex.Match(originalText, @"(<#\w{6}>.*?</color>)");

        if (match.Success)
        {
            blinkingWord = match.Value; // Guardar la palabra con su color original
            hiddenWord = "<color=#00000000>" + Regex.Replace(blinkingWord, @"<.*?>", "") + "</color>"; // Convertir en transparente
            baseText = originalText.Replace(blinkingWord, "%%%"); // Reemplazo temporal para reconstrucción
        }
        else
        {
            Debug.LogError("No se encontró una palabra con formato de color en el texto.");
        }
    }

    private IEnumerator BlinkCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < blinkDuration)
        {
            textComponent.text = baseText.Replace("%%%", hiddenWord); // Oculta la palabra
            yield return new WaitForSeconds(blinkSpeed);

            textComponent.text = baseText.Replace("%%%", blinkingWord); // Muestra la palabra
            yield return new WaitForSeconds(blinkSpeed);

            elapsedTime += blinkSpeed * 2;
        }

        textComponent.text = baseText.Replace("%%%", blinkingWord); // Dejar la palabra visible al final
    }
}

