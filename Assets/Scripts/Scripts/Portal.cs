using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Portal Configuration")]
    public GridCell linkedCell;

    private Color initialColor;
    private bool isActivated = false;
    private SpriteRenderer spriteRenderer;

    private static List<Portal> activePortals = new List<Portal>();

    private void Start()
    {
        InitializePortal();
    }

    private void OnDestroy()
    {
        CleanupPortal();
    }

    public void OnCellColorChanged(Color newColor)
    {
        if (ShouldIgnoreColorChange(newColor))
            return;

        ActivatePortal(newColor);
    }

    private void InitializePortal()
    {
        RegisterPortal();
        CacheComponents();
        SetupInitialState();
    }

    private void RegisterPortal()
    {
        activePortals.Add(this);
    }

    private void CacheComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void SetupInitialState()
    {
        if (HasValidLinkedCell())
        {
            InitializeColorFromLinkedCell();
        }
        else
        {
            LogMissingLinkedCellWarning();
        }
    }

    private bool HasValidLinkedCell()
    {
        return linkedCell != null;
    }

    private void InitializeColorFromLinkedCell()
    {
        initialColor = linkedCell.GetCurrentColor();
        ApplyInitialColorToSprite();
    }

    private void ApplyInitialColorToSprite()
    {
        if (HasSpriteRenderer())
        {
            spriteRenderer.color = initialColor;
        }
    }

    private bool HasSpriteRenderer()
    {
        return spriteRenderer != null;
    }

    private void LogMissingLinkedCellWarning()
    {
        Debug.LogWarning("Portal sin celda vinculada.");
    }

    private bool ShouldIgnoreColorChange(Color newColor)
    {
        return isActivated || newColor.Equals(initialColor);
    }

    private void ActivatePortal(Color colorToPropagate)
    {
        MarkAsActivated();
        HidePortalSprite();
        UnlinkFromCell();
        RemoveFromActivePortals();
        PropagateColorToOtherPortals(colorToPropagate);
        DestroyPortal();
    }

    private void MarkAsActivated()
    {
        isActivated = true;
    }

    private void HidePortalSprite()
    {
        if (HasSpriteRenderer())
        {
            spriteRenderer.enabled = false;
        }
    }

    private void UnlinkFromCell()
    {
        if (HasValidLinkedCell())
        {
            linkedCell.linkedPortal = null;
        }
    }

    private void RemoveFromActivePortals()
    {
        activePortals.Remove(this);
    }

    private void PropagateColorToOtherPortals(Color colorToPropagate)
    {
        List<Portal> portalsCopy = new List<Portal>(activePortals);

        foreach (Portal portal in portalsCopy)
        {
            if (ShouldPropagateToPortal(portal))
            {
                StartColorPropagationOnPortal(portal, colorToPropagate);
            }
        }
    }

    private bool ShouldPropagateToPortal(Portal portal)
    {
        return !portal.isActivated && portal.linkedCell != null;
    }

    private void StartColorPropagationOnPortal(Portal portal, Color colorToPropagate)
    {
        portal.linkedCell.StartCoroutine(
            portal.linkedCell.PropagateColorGradually(colorToPropagate)
        );
    }

    private void DestroyPortal()
    {
        Destroy(gameObject);
    }

    private void CleanupPortal()
    {
        activePortals.Remove(this);
    }
}