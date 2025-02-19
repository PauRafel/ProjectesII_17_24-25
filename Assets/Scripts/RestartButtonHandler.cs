using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtonHandler : MonoBehaviour
{

    [SerializeField] private LevelTimer levelTimer;

    private void Start()
    {
        levelTimer = FindObjectOfType<LevelTimer>();

        if (levelTimer != null)
        {
            levelTimer.saveACtualTime();
        }
    }
    public void RestartLevel()
    {
        // Recargar la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}