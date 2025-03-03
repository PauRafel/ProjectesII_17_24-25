using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class TutorialManager : MonoBehaviour
{
    public GameObject tutorialPanel; // Panel oscuro que cubre la pantalla
    public TMP_Text tutorialText; // Texto explicativo del tutorial

    private int step = 0;

    private void Start()
    {
        tutorialPanel.SetActive(true); // Oscurece la pantalla
        ShowStep();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Detecta clics en cualquier parte
        {
            NextStep();
        }
    }

    private void ShowStep()
    {
        switch (step)
        {
            case 0:
                tutorialText.text = "Welcome to Chromatic!";
                // mensaje de bienvenida
                break;
            case 1:
                tutorialText.text = "Select a color and dye the blocks of a different color with it";
                // Aquí podemos resaltar selección de colores
                break;
            case 2:
                tutorialText.text = "This is the palette where you will apply the colors by clicking on the boxes!";
                // Resaltar grid
                break;
            case 3:
                tutorialText.text = "View the target color here!";
                // Resaltar el mensaje de "Target Color"
                break;
            case 4:
                tutorialText.text = "Dye all blocks to match the target color within the limited steps!";
                // Resaltar contador de intentos restantes
                break;
            case 5:
                tutorialText.text = "Ready? Relax and enjoy!";
                // Mensaje final
                break;
            case 6:
                tutorialPanel.SetActive(false); // Finaliza el tutorial
                return;
        }
    }

    private void NextStep()
    {
        step++;
        ShowStep();
    }
}
