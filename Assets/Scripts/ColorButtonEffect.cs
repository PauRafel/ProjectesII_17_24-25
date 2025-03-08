using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorButtonEffect : MonoBehaviour
{
    private Vector3 originalScale;
    private Outline outlineEffect;
    private Button button;
    private static ColorButtonEffect selectedButton; // Referencia al botón seleccionado

    void Start()
    {
        originalScale = transform.localScale;
        outlineEffect = GetComponent<Outline>();
        button = GetComponent<Button>();

        if (outlineEffect == null)
        {
            outlineEffect = gameObject.AddComponent<Outline>(); // Agrega un borde si no tiene
        }

        outlineEffect.effectColor = new Color(1f, 1f, 0f, 0.8f); // Amarillo con algo de transparencia
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
    }

    private void ResetButton()
    {
        transform.localScale = originalScale; // Restablece el tamaño original
        outlineEffect.enabled = false; // Desactiva el brillo
    }
}
