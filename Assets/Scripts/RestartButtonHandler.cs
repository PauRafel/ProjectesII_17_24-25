using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtonHandler : MonoBehaviour
{
    public void RestartLevel()
    {
        // Recargar la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}