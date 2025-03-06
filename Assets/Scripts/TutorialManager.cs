using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static bool isTutorialActive = true; // Bloquea la interacción mientras el tutorial está activo

    public GameObject[] tutorialPanels; // Array con los paneles de cada paso
    public TMP_Text tutorialText0;
    public TMP_Text tutorialText1;
    public TMP_Text tutorialText2;
    public TMP_Text tutorialText3;
    public TMP_Text tutorialText4;
    public TMP_Text tutorialText5;

    private int step = 0;

    void Start()
    {
        isTutorialActive = true;  // Bloqueamos la interacción
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
        // Desactivar todos los paneles antes de activar el actual
        foreach (GameObject panel in tutorialPanels)
        {
            panel.SetActive(false);
        }

        switch (step)
        {
            case 0:
                tutorialText0.text = "Welcome to Chromatic!";
                tutorialPanels[0].SetActive(true);
                break;
            case 1:
                tutorialText1.text = "Select a color and dye the blocks of a different color with it";
                tutorialPanels[1].SetActive(true);
                break;
            case 2:
                tutorialText2.text = "This is the palette where you will apply the colors by clicking on the boxes!";
                tutorialPanels[2].SetActive(true);
                break;
            case 3:
                tutorialText3.text = "View the target color here!";
                tutorialPanels[3].SetActive(true);
                break;
            case 4:
                tutorialText4.text = "Dye all blocks to match the target color within the limited steps!";
                tutorialPanels[4].SetActive(true);
                break;
            case 5:
                tutorialText5.text = "Ready? Relax and enjoy!";
                tutorialPanels[5].SetActive(true);
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
}
