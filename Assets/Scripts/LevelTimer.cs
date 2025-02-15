using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText; // Referencia al texto en la UI
    private float elapsedTime = 0f;
    private bool isRunning = true;
    private string levelKey;

    void Start()
    {
        levelKey = "BestTime_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StopTimer()
    {
        isRunning = false;
        SaveBestTime();
    }

    void SaveBestTime()
    {
        float bestTime = PlayerPrefs.GetFloat(levelKey, float.MaxValue);

        if (elapsedTime < bestTime)
        {
            PlayerPrefs.SetFloat(levelKey, elapsedTime);
            PlayerPrefs.Save();
            VictoryUI.Instance.ShowBestTime(elapsedTime, true);
        }
        else
        {
            VictoryUI.Instance.ShowBestTime(elapsedTime, false);
        }
    }
}
