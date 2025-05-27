using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static bool isTutorialActive = false;

    [Header("UI Components")]
    public GameObject[] tutorialPanels;
    public Image[] tutorialBorders;

    [Header("Tutorial Texts")]
    public TMP_Text tutorialText0;
    public TMP_Text tutorialText1;
    public TMP_Text tutorialText2;
    public TMP_Text tutorialText3;
    public TMP_Text tutorialText4;

    private TMP_Text[] tutorialTexts;
    private int currentStep = 0;
    private Coroutine fadeCoroutine;
    private Coroutine borderBlinkCoroutine;

    private const float FADE_IN_DURATION = 1.2f;
    private const float FADE_OUT_DURATION = 0.6f;
    private const float BORDER_BLINK_DURATION = 1f;
    private const int TOTAL_TUTORIAL_STEPS = 6;

    private readonly string[] tutorialMessages = {
        "Welcome to Chromatic!",
        "Select a color and dye the blocks of a different color with it",
        "This is the palette! Apply the colors by clicking on the boxes!",
        "The color of the line and the number of the level indicate the target!",
        "Dye all blocks to match the target color within the limited steps!"
    };

    private void Start()
    {
        InitializeTutorial();
        ShowCurrentStep();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AdvanceToNextStep();
        }
    }

    private void InitializeTutorial()
    {
        isTutorialActive = true;
        InitializeTutorialTexts();
        ResetTextAlpha();
    }

    private void InitializeTutorialTexts()
    {
        tutorialTexts = new TMP_Text[] {
            tutorialText0,
            tutorialText1,
            tutorialText2,
            tutorialText3,
            tutorialText4
        };
    }

    private void ResetTextAlpha()
    {
        foreach (TMP_Text text in tutorialTexts)
        {
            text.alpha = 0;
        }
    }

    private void ShowCurrentStep()
    {
        if (HasTutorialEnded())
        {
            EndTutorial();
            return;
        }

        PrepareStepTransition();
        ExecuteCurrentStep();
    }

    private bool HasTutorialEnded()
    {
        return currentStep >= TOTAL_TUTORIAL_STEPS;
    }

    private void PrepareStepTransition()
    {
        DeactivateAllPanels();
        DeactivateAllBorders();
        StopCurrentFadeCoroutine();
        FadeOutPreviousText();
    }

    private void DeactivateAllPanels()
    {
        foreach (GameObject panel in tutorialPanels)
        {
            panel.SetActive(false);
        }
    }

    private void DeactivateAllBorders()
    {
        foreach (Image border in tutorialBorders)
        {
            border.gameObject.SetActive(false);
        }
    }

    private void StopCurrentFadeCoroutine()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }

    private void FadeOutPreviousText()
    {
        if (currentStep > 0)
        {
            fadeCoroutine = StartCoroutine(FadeTextOut(tutorialTexts[currentStep - 1]));
        }
    }

    private void ExecuteCurrentStep()
    {
        var stepConfig = GetStepConfiguration(currentStep);

        if (stepConfig.IsEndStep)
        {
            FinalizeTutorial();
            return;
        }

        DisplayStepContent(stepConfig);
    }

    private TutorialStepConfig GetStepConfiguration(int step)
    {
        return step switch
        {
            0 => new TutorialStepConfig { PanelIndex = 0, BorderIndex = -1, MessageIndex = 0 },
            1 => new TutorialStepConfig { PanelIndex = 1, BorderIndex = 0, MessageIndex = 1 },
            2 => new TutorialStepConfig { PanelIndex = 2, BorderIndex = 1, MessageIndex = 2 },
            3 => new TutorialStepConfig { PanelIndex = 3, BorderIndex = 2, MessageIndex = 3 },
            4 => new TutorialStepConfig { PanelIndex = 4, BorderIndex = 3, MessageIndex = 4 },
            5 => new TutorialStepConfig { IsEndStep = true },
            _ => new TutorialStepConfig { IsEndStep = true }
        };
    }

    private void DisplayStepContent(TutorialStepConfig config)
    {
        SetTutorialMessage(config.MessageIndex);
        ActivateStepPanel(config.PanelIndex);
        HandleStepBorder(config.BorderIndex);
        StartTextFadeIn(config.MessageIndex);
    }

    private void SetTutorialMessage(int messageIndex)
    {
        tutorialTexts[messageIndex].text = tutorialMessages[messageIndex];
    }

    private void ActivateStepPanel(int panelIndex)
    {
        tutorialPanels[panelIndex].SetActive(true);
    }

    private void HandleStepBorder(int borderIndex)
    {
        if (borderIndex >= 0)
        {
            ActivateBorderWithBlink(borderIndex);
        }
    }

    private void ActivateBorderWithBlink(int borderIndex)
    {
        tutorialBorders[borderIndex].gameObject.SetActive(true);
        StopCurrentBorderBlink();
        StartBorderBlink(borderIndex);
    }

    private void StopCurrentBorderBlink()
    {
        if (borderBlinkCoroutine != null)
        {
            StopCoroutine(borderBlinkCoroutine);
        }
    }

    private void StartBorderBlink(int borderIndex)
    {
        borderBlinkCoroutine = StartCoroutine(BlinkBorder(tutorialBorders[borderIndex]));
    }

    private void StartTextFadeIn(int textIndex)
    {
        fadeCoroutine = StartCoroutine(FadeTextIn(tutorialTexts[textIndex]));
    }

    private void FinalizeTutorial()
    {
        DeactivateAllPanels();
        EndTutorial();
    }

    private void EndTutorial()
    {
        isTutorialActive = false;
    }

    private void AdvanceToNextStep()
    {
        currentStep++;
        ShowCurrentStep();
    }

    private IEnumerator FadeTextIn(TMP_Text text)
    {
        yield return AnimateTextAlpha(text, 0f, 1f, FADE_IN_DURATION);
    }

    private IEnumerator FadeTextOut(TMP_Text text)
    {
        yield return AnimateTextAlpha(text, 1f, 0f, FADE_OUT_DURATION);
    }

    private IEnumerator AnimateTextAlpha(TMP_Text text, float startAlpha, float endAlpha, float duration)
    {
        text.alpha = startAlpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        text.alpha = endAlpha;
    }

    private IEnumerator BlinkBorder(Image border)
    {
        float elapsed = 0f;

        while (border.gameObject.activeSelf)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.PingPong(elapsed / BORDER_BLINK_DURATION, 1f);
            SetBorderAlpha(border, alpha);
            yield return null;
        }
    }

    private void SetBorderAlpha(Image border, float alpha)
    {
        Color color = border.color;
        color.a = alpha;
        border.color = color;
    }

    private struct TutorialStepConfig
    {
        public int PanelIndex;
        public int BorderIndex;
        public int MessageIndex;
        public bool IsEndStep;
    }
}