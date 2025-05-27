using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorButtonEffect : MonoBehaviour
{
    private const float SCALE_MULTIPLIER = 1.15f;
    private const float PULSE_SPEED = 2f;
    private const float MIN_OUTLINE_ALPHA = 0.3f;
    private const float MAX_OUTLINE_ALPHA = 0.8f;
    private const float OUTLINE_DISTANCE = 5f;

    private static readonly Color OUTLINE_COLOR = new Color(1f, 1f, 0f, 1f);
    private static readonly Vector2 OUTLINE_EFFECT_DISTANCE = new Vector2(OUTLINE_DISTANCE, -OUTLINE_DISTANCE);

    private Vector3 originalScale;
    private Outline outlineEffect;
    private Button button;
    private Coroutine pulseCoroutine;

    private static ColorButtonEffect selectedButton;

    private void Start()
    {
        InitializeComponents();
        SetupOutlineEffect();
        RegisterButtonCallback();
    }

    private void InitializeComponents()
    {
        StoreOriginalScale();
        GetComponentReferences();
        EnsureOutlineExists();
    }

    private void StoreOriginalScale()
    {
        originalScale = transform.localScale;
    }

    private void GetComponentReferences()
    {
        outlineEffect = GetComponent<Outline>();
        button = GetComponent<Button>();
    }

    private void EnsureOutlineExists()
    {
        if (IsOutlineEffectMissing())
        {
            CreateOutlineEffect();
        }
    }

    private bool IsOutlineEffectMissing()
    {
        return outlineEffect == null;
    }

    private void CreateOutlineEffect()
    {
        outlineEffect = gameObject.AddComponent<Outline>();
    }

    private void SetupOutlineEffect()
    {
        ConfigureOutlineAppearance();
        DisableOutlineByDefault();
    }

    private void ConfigureOutlineAppearance()
    {
        outlineEffect.effectColor = OUTLINE_COLOR;
        outlineEffect.effectDistance = OUTLINE_EFFECT_DISTANCE;
    }

    private void DisableOutlineByDefault()
    {
        outlineEffect.enabled = false;
    }

    private void RegisterButtonCallback()
    {
        button.onClick.AddListener(SelectButton);
    }

    private void SelectButton()
    {
        DeselectPreviousButton();
        SetAsSelectedButton();
        ApplySelectionEffects();
        StartPulseAnimation();
    }

    private void DeselectPreviousButton()
    {
        if (HasSelectedButton())
        {
            selectedButton.ResetButton();
        }
    }

    private bool HasSelectedButton()
    {
        return selectedButton != null;
    }

    private void SetAsSelectedButton()
    {
        selectedButton = this;
    }

    private void ApplySelectionEffects()
    {
        ScaleButton();
        EnableOutline();
    }

    private void ScaleButton()
    {
        transform.localScale = CalculateScaledSize();
    }

    private Vector3 CalculateScaledSize()
    {
        return originalScale * SCALE_MULTIPLIER;
    }

    private void EnableOutline()
    {
        outlineEffect.enabled = true;
    }

    private void StartPulseAnimation()
    {
        StopCurrentPulse();
        pulseCoroutine = StartCoroutine(PulseEffect());
    }

    private void StopCurrentPulse()
    {
        if (IsPulseActive())
        {
            StopCoroutine(pulseCoroutine);
        }
    }

    private bool IsPulseActive()
    {
        return pulseCoroutine != null;
    }

    private void ResetButton()
    {
        RestoreOriginalScale();
        DisableOutline();
        StopPulseEffect();
    }

    private void RestoreOriginalScale()
    {
        transform.localScale = originalScale;
    }

    private void DisableOutline()
    {
        outlineEffect.enabled = false;
    }

    private void StopPulseEffect()
    {
        if (IsPulseActive())
        {
            StopCoroutine(pulseCoroutine);
            ClearPulseReference();
        }
    }

    private void ClearPulseReference()
    {
        pulseCoroutine = null;
    }

    private IEnumerator PulseEffect()
    {
        while (true)
        {
            yield return FadeOutlineUp();
            yield return FadeOutlineDown();
        }
    }

    private IEnumerator FadeOutlineUp()
    {
        return FadeOutline(MIN_OUTLINE_ALPHA, MAX_OUTLINE_ALPHA);
    }

    private IEnumerator FadeOutlineDown()
    {
        return FadeOutline(MAX_OUTLINE_ALPHA, MIN_OUTLINE_ALPHA);
    }

    private IEnumerator FadeOutline(float startAlpha, float endAlpha)
    {
        for (float t = 0; t <= 1; t += Time.deltaTime * PULSE_SPEED)
        {
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
            UpdateOutlineAlpha(currentAlpha);
            yield return null;
        }
    }

    private void UpdateOutlineAlpha(float alpha)
    {
        outlineEffect.effectColor = new Color(1f, 1f, 0f, alpha);
    }
}