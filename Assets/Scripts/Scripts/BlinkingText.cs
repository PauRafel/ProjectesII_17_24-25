using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class BlinkingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private float blinkDuration = 5f;
    [SerializeField] private float blinkSpeed = 0.5f;

    private const string COLOR_PATTERN = @"(<#\w{6}>.*?</color>)";
    private const string PLACEHOLDER = "%%%";
    private const string TRANSPARENT_COLOR = "#00000000";

    private string originalText;
    private string blinkingWord;
    private string hiddenWord;
    private string baseText;

    private void Start()
    {
        InitializeTextComponent();
        if (IsTextComponentValid())
        {
            PrepareBlinkingText();
            StartBlinking();
        }
    }

    private void InitializeTextComponent()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
    }

    private bool IsTextComponentValid()
    {
        return textComponent != null;
    }

    private void PrepareBlinkingText()
    {
        originalText = textComponent.text;
        ExtractBlinkingElements();
    }

    private void StartBlinking()
    {
        StartCoroutine(BlinkCoroutine());
    }

    private void ExtractBlinkingElements()
    {
        Match colorMatch = FindColoredWord();

        if (colorMatch.Success)
        {
            ProcessColoredWord(colorMatch);
        }
        else
        {
            LogColorFormatError();
        }
    }

    private Match FindColoredWord()
    {
        return Regex.Match(originalText, COLOR_PATTERN);
    }

    private void ProcessColoredWord(Match match)
    {
        blinkingWord = match.Value;
        hiddenWord = CreateHiddenVersion(blinkingWord);
        baseText = CreateBaseText(blinkingWord);
    }

    private string CreateHiddenVersion(string coloredWord)
    {
        string cleanWord = RemoveColorTags(coloredWord);
        return WrapWithTransparentColor(cleanWord);
    }

    private string RemoveColorTags(string text)
    {
        return Regex.Replace(text, @"<.*?>", "");
    }

    private string WrapWithTransparentColor(string text)
    {
        return $"<color={TRANSPARENT_COLOR}>{text}</color>";
    }

    private string CreateBaseText(string wordToReplace)
    {
        return originalText.Replace(wordToReplace, PLACEHOLDER);
    }

    private void LogColorFormatError()
    {
        Debug.LogError("No se encontró una palabra con formato de color en el texto.");
    }

    private IEnumerator BlinkCoroutine()
    {
        float elapsedTime = 0f;
        float cycleTime = CalculateCycleTime();

        while (elapsedTime < blinkDuration)
        {
            yield return PerformBlinkCycle();
            elapsedTime += cycleTime;
        }

        ShowFinalState();
    }

    private float CalculateCycleTime()
    {
        return blinkSpeed * 2;
    }

    private IEnumerator PerformBlinkCycle()
    {
        HideWord();
        yield return new WaitForSeconds(blinkSpeed);

        ShowWord();
        yield return new WaitForSeconds(blinkSpeed);
    }

    private void HideWord()
    {
        SetText(hiddenWord);
    }

    private void ShowWord()
    {
        SetText(blinkingWord);
    }

    private void SetText(string wordToShow)
    {
        textComponent.text = baseText.Replace(PLACEHOLDER, wordToShow);
    }

    private void ShowFinalState()
    {
        SetText(blinkingWord);
    }
}