using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class TutorialManager : MonoBehaviour
{
    private Coroutine fadeCoroutine;

    public GameObject colorSelectionUI;  // Panel de selección de colores
    public GameObject grid;              // La cuadrícula del juego
    public GameObject targetColorUI;     // Mensaje del color objetivo
    public GameObject attemptsUI;        // Contador de intentos


    public GameObject tutorialPanel; // Panel oscuro que cubre la pantalla
    public TMP_Text tutorialText; // Texto explicativo del tutorial


    public GameObject highlightArea; // Referencia al área de resaltado
    public RectTransform highlightRect; // Para cambiar tamaño y posición


    private int step = 0;

    void Start()
    {
        tutorialPanel.SetActive(true); // Oscurece la pantalla
        colorSelectionUI.SetActive(false);
        grid.SetActive(false);
        targetColorUI.SetActive(false);
        attemptsUI.SetActive(false);
        ShowStep();
    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Detecta clics en cualquier parte
        {
            NextStep();
        }
    }
    private void NextStep()
    {
        step++;
        ShowStep();
    }


    private void HighlightElement(GameObject element)
    {
        tutorialPanel.SetActive(true);  // Asegurar que el panel oscuro sigue activo

        // Oscurece todo excepto el objeto resaltado
        colorSelectionUI.SetActive(false);
        grid.SetActive(false);
        targetColorUI.SetActive(false);
        attemptsUI.SetActive(false);

        if (element != null)
        {
            element.SetActive(true);
        }
    }

    private IEnumerator FadeTextIn()
    {
        tutorialText.alpha = 0;
        float duration = 0.5f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tutorialText.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
    }

  

    private void ShowStep()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeTextIn());


        switch (step)
        {
            case 0:
                tutorialText.text = "Welcome to Chromatic!";
                HighlightElement(null);  // Solo muestra el mensaje de bienvenida
                highlightArea.SetActive(false); // Nada resaltado
                break;
            case 1:
                tutorialText.text = "Select a color and dye the blocks of a different color with it";
                HighlightElement(colorSelectionUI);
                highlightArea.SetActive(true);
                highlightRect.anchoredPosition = new Vector2(-56, -472); // Ajusta según posición de la paleta
                highlightRect.sizeDelta = new Vector2(373, 115);
                break;
            case 2:
                tutorialText.text = "This is the palette where you will apply the colors by clicking on the boxes!";
                HighlightElement(grid);
                highlightRect.anchoredPosition = new Vector2(0, -13); // Ajusta según la cuadrícula
                highlightRect.sizeDelta = new Vector2(476, 656);
                break;
            case 3:
                tutorialText.text = "View the target color here!";
                HighlightElement(targetColorUI);
                highlightRect.anchoredPosition = new Vector2(0, 390);
                highlightRect.sizeDelta = new Vector2(423, 26);
                break;
            case 4:
                tutorialText.text = "Dye all blocks to match the target color within the limited steps!";
                HighlightElement(attemptsUI);
                highlightRect.anchoredPosition = new Vector2(193, -467);
                highlightRect.sizeDelta = new Vector2(100, 100);
                break;
            case 5:
                tutorialText.text = "Ready? Relax and enjoy!";
                HighlightElement(null);
                highlightArea.SetActive(false); // Ocultamos resalte
                break;
            case 6:
                tutorialPanel.SetActive(false); // Finaliza el tutorial

                colorSelectionUI.SetActive(true);
                grid.SetActive(true);
                targetColorUI.SetActive(true);
                attemptsUI.SetActive(true);
                return;
        }
    }

   
}
