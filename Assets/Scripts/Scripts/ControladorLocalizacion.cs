using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class ControladorLocalizacion : MonoBehaviour
{
    private const string LOCALE_KEY = "LocaleKey";
    private const int DEFAULT_LOCALE_ID = 0;

    private bool _active = false;

    private void Start()
    {
        InitializeLocalization();
    }

    public void ChangeLocale(int localeID)
    {
        if (IsLocalizationInProgress()) return;

        StartLocaleChange(localeID);
    }

    private void InitializeLocalization()
    {
        int savedLocaleId = GetSavedLocaleId();
        ChangeLocale(savedLocaleId);
    }

    private int GetSavedLocaleId()
    {
        return PlayerPrefs.GetInt(LOCALE_KEY, DEFAULT_LOCALE_ID);
    }

    private bool IsLocalizationInProgress()
    {
        return _active;
    }

    private void StartLocaleChange(int localeID)
    {
        StartCoroutine(SetLocale(localeID));
    }

    private IEnumerator SetLocale(int localeID)
    {
        SetLocalizationActive(true);

        yield return WaitForLocalizationInitialization();

        ApplyLocaleChange(localeID);
        SaveLocalePreference(localeID);

        SetLocalizationActive(false);
    }

    private void SetLocalizationActive(bool active)
    {
        _active = active;
    }

    private IEnumerator WaitForLocalizationInitialization()
    {
        yield return LocalizationSettings.InitializationOperation;
    }

    private void ApplyLocaleChange(int localeID)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
    }

    private void SaveLocalePreference(int localeID)
    {
        PlayerPrefs.SetInt(LOCALE_KEY, localeID);
    }
}