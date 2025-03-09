using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static bool isTutorialActive = true; // Bloquea la interacción mientras el tutorial está activo

    public GameObject[] tutorialPanels; // Array con los paneles de cada paso
    public Image[] tutorialBorders; // Array con los bordes de cada panel

    public TMP_Text tutorialText0;
    public TMP_Text tutorialText1;
    public TMP_Text tutorialText2;
    public TMP_Text tutorialText3;
    public TMP_Text tutorialText4;
    public TMP_Text tutorialText5;

    private TMP_Text[] tutorialTexts;
    private int step = 0;

    private Coroutine fadeCoroutine;
    private Coroutine borderBlinkCoroutine;

    void Start()
    {
        isTutorialActive = true;

        // Guardar los textos en un array para fácil acceso
        tutorialTexts = new TMP_Text[] { tutorialText0, tutorialText1, tutorialText2, tutorialText3, tutorialText4, tutorialText5 };

        // Asegurar que los textos comiencen con alpha en 0
        foreach (TMP_Text text in tutorialTexts)
        {
            text.alpha = 0;
        }

        ShowStep();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            NextStep();
        }
    }

    private void ShowStep()
    {
        if (step >= 7)
        {
            isTutorialActive = false;
            return;
        }
        // Desactivar todos los paneles antes de activar el actual
        foreach (GameObject panel in tutorialPanels)
        {
            panel.SetActive(false);
        }

        foreach (Image border in tutorialBorders)
        {
            border.gameObject.SetActive(false);
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Hacer fade out del texto anterior antes de avanzar
        if (step > 0)
        {
            fadeCoroutine = StartCoroutine(FadeTextOut(tutorialTexts[step - 1]));
        }

        switch (step)
        {
            case 0:
                tutorialText0.text = "Welcome to Chromatic!";
                tutorialPanels[0].SetActive(true);
                fadeCoroutine = StartCoroutine(FadeTextIn(tutorialText0));
                break;
            case 1:
                tutorialText1.text = "Select a color and dye the blocks of a different color with it";
                tutorialPanels[1].SetActive(true);
                tutorialBorders[0].gameObject.SetActive(true);
                fadeCoroutine = StartCoroutine(FadeTextIn(tutorialText1));
                if (borderBlinkCoroutine != null)
                {
                    StopCoroutine(borderBlinkCoroutine);
                }
                borderBlinkCoroutine = StartCoroutine(BlinkBorder(tutorialBorders[0]));
                break;
            case 2:
                tutorialText2.text = "This is the palette! Apply the colors by clicking on the boxes!";
                tutorialPanels[2].SetActive(true);
                tutorialBorders[1].gameObject.SetActive(true);
                fadeCoroutine = StartCoroutine(FadeTextIn(tutorialText2));
                if (borderBlinkCoroutine != null)
                {
                    StopCoroutine(borderBlinkCoroutine);
                }
                borderBlinkCoroutine = StartCoroutine(BlinkBorder(tutorialBorders[1]));
                break;
            case 3:
                tutorialText3.text = "View the target color here!";
                tutorialPanels[3].SetActive(true);
                tutorialBorders[2].gameObject.SetActive(true);
                fadeCoroutine = StartCoroutine(FadeTextIn(tutorialText3));
                if (borderBlinkCoroutine != null)
                {
                    StopCoroutine(borderBlinkCoroutine);
                }
                borderBlinkCoroutine = StartCoroutine(BlinkBorder(tutorialBorders[2]));
                break;
            case 4:
                tutorialText4.text = "Dye all blocks to match the target color within the limited steps!";
                tutorialPanels[4].SetActive(true);
                tutorialBorders[3].gameObject.SetActive(true);
                fadeCoroutine = StartCoroutine(FadeTextIn(tutorialText4));
                if (borderBlinkCoroutine != null)
                {
                    StopCoroutine(borderBlinkCoroutine);
                }
                borderBlinkCoroutine = StartCoroutine(BlinkBorder(tutorialBorders[3]));
                break;
            case 5:
                tutorialText5.text = "Ready? Relax and enjoy!";
                tutorialPanels[5].SetActive(true);
                fadeCoroutine = StartCoroutine(FadeTextIn(tutorialText5));
                break;
            case 6:
                foreach (GameObject panel in tutorialPanels)
                {
                    panel.SetActive(false); // Desactivar todos los paneles al terminar
                    isTutorialActive = false;  // Habilitamos la interacción
                }
                return;
        }
    }

    private void NextStep()
    {
        step++;
        ShowStep();
    }

    private IEnumerator FadeTextIn(TMP_Text text)
    {
        text.alpha = 0;
        float duration = 1.2f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }

        text.alpha = 1;
    }

    private IEnumerator FadeTextOut(TMP_Text text)
    {
        float duration = 0.6f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }

        text.alpha = 0;
    }

    private IEnumerator BlinkBorder(Image border)
    {
        float duration = 1f;
        float elapsed = 0;

        while (border.gameObject.activeSelf)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.PingPong(elapsed / duration, 1); // Oscila entre 0 y 1

            Color color = border.color;
            color.a = alpha;
            border.color = color;

            yield return null;
        }
    }
}
