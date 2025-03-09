using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorButtonEffect : MonoBehaviour
{
    private Vector3 originalScale;
    private Outline outlineEffect;
    private Button button;
    private static ColorButtonEffect selectedButton; // Referencia al botón seleccionado
    private Coroutine pulseCoroutine;

    void Start()
    {
        originalScale = transform.localScale;
        outlineEffect = GetComponent<Outline>();
        button = GetComponent<Button>();

        if (outlineEffect == null)
        {
            outlineEffect = gameObject.AddComponent<Outline>(); // Agrega un borde si no tiene
        }

        outlineEffect.effectColor = new Color(1f, 1f, 0f, 1f); // Amarillo con algo de transparencia
        outlineEffect.effectDistance = new Vector2(5, -5); // Tamaño del brillo
        outlineEffect.enabled = false; // Desactivado por defecto

        button.onClick.AddListener(SelectButton);
    }

    private void SelectButton()
    {
        if (selectedButton != null)
        {
            selectedButton.ResetButton();
        }

        selectedButton = this;
        transform.localScale = originalScale * 1.2f; // Aumenta el tamaño un 20%
        outlineEffect.enabled = true; // Activa el brillo

        // Iniciar animación de parpadeo y pulso
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(PulseEffect());
    }

    private void ResetButton()
    {
        transform.localScale = originalScale; // Restablece el tamaño original
        outlineEffect.enabled = false; // Desactiva el brillo

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private IEnumerator PulseEffect()
    {
        while (true)
        {
            // Animación de parpadeo del borde
            for (float t = 0; t <= 1; t += Time.deltaTime * 2)
            {
                outlineEffect.effectColor = new Color(1f, 1f, 0f, Mathf.Lerp(0.3f, 0.8f, t));
                yield return null;
            }
            for (float t = 0; t <= 1; t += Time.deltaTime * 2)
            {
                outlineEffect.effectColor = new Color(1f, 1f, 0f, Mathf.Lerp(0.8f, 0.3f, t));
                yield return null;
            }
        }
    }
}
