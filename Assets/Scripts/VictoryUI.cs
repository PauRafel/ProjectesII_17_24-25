using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VictoryUI : MonoBehaviour
{
    public static VictoryUI Instance;
    public TextMeshProUGUI finalTimeText;
    public GameObject newRecordText;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowBestTime(float time, bool isNewRecord)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        finalTimeText.text = $"Tiempo: {minutes:00}:{seconds:00}";

        newRecordText.SetActive(isNewRecord);
    }
}
