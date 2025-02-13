using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VictoryEffects : MonoBehaviour
{
    public AudioSource victorySound;
    public float glowDuration = 1.5f;
    public Color glowColor = Color.yellow;
    private Color originalColor;
    private SpriteRenderer[] gridCells;

    void Start()
    {
        gridCells = FindObjectsOfType<SpriteRenderer>();
    }

    public void PlayVictoryEffects()
    {
        if (victorySound)
            victorySound.Play();

        StartCoroutine(GlowEffect());
    }

    private IEnumerator GlowEffect()
    {
        float elapsedTime = 0;
        while (elapsedTime < glowDuration)
        {
            float lerpFactor = Mathf.PingPong(elapsedTime * 2, 1);
            foreach (SpriteRenderer cell in gridCells)
            {
                cell.color = Color.Lerp(originalColor, glowColor, lerpFactor);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        foreach (SpriteRenderer cell in gridCells)
        {
            cell.color = originalColor;
        }
    }
}

