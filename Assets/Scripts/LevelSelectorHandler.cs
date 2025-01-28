using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectorHandeler : MonoBehaviour
{
    public void ChangeLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void ChangeLevel(int levelNumber)
    {
        SceneManager.LoadScene(levelNumber);
    }
}