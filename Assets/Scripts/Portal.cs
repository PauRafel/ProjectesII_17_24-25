using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public GridCell linkedCell;
    private Color initialColor;
    private bool isActivated = false;

    private static List<Portal> activePortals = new List<Portal>();

    void Start()
    {
        activePortals.Add(this);
        if (linkedCell != null)
        {
            initialColor = linkedCell.GetCurrentColor();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = initialColor;
        }
        else
        {
            Debug.LogWarning("Portal sin celda vinculada.");
        }
    }

    public void OnCellColorChanged(Color newColor)
    {
        if (isActivated) return; 
        if (newColor.Equals(initialColor)) return; 

        ActivatePortal(newColor);
    }

    private void ActivatePortal(Color colorToPropagate)
    {
        isActivated = true; 

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (linkedCell != null)
        {
            linkedCell.linkedPortal = null;
        }

        activePortals.Remove(this);

        foreach (Portal otherPortal in new List<Portal>(activePortals))
        {
            if (!otherPortal.isActivated && otherPortal.linkedCell != null)
            {
                otherPortal.linkedCell.StartCoroutine(otherPortal.linkedCell.PropagateColorGradually(colorToPropagate));

            }
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        activePortals.Remove(this);
    }
}
