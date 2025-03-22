using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    // Referencia a la celda de la cuadr�cula que este Portal supervisa
    public GridCell linkedCell;
    // Color inicial de la celda (y del Portal) al comenzar el nivel
    private Color initialColor;
    // Indicador de si este Portal ya fue activado/eliminado
    private bool isActivated = false;

    // Lista est�tica de todos los Portales activos en la escena
    private static List<Portal> activePortals = new List<Portal>();

    void Start()
    {
        // Registrar este Portal en la lista de activos
        activePortals.Add(this);
        // Tomar el color inicial de la celda vinculada y aplicarlo al sprite del Portal
        if (linkedCell != null)
        {
            initialColor = linkedCell.GetCurrentColor();  // Suponiendo que GridCell tiene un m�todo para obtener su color actual
            // Asignar color al sprite renderer del Portal para que coincida con el de la celda
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = initialColor;
        }
        else
        {
            Debug.LogWarning("Portal sin celda vinculada.");
        }
    }

    // M�todo llamado por GridCell cuando su color cambia
    public void OnCellColorChanged(Color newColor)
    {
        if (isActivated) return; // Si ya fue activado, no hacer nada
        if (newColor.Equals(initialColor)) return; // Si no hubo cambio real de color, no activar

        // Activar el Portal porque la celda debajo cambi� de color
        ActivatePortal(newColor);
    }

    // L�gica de activaci�n del Portal
    private void ActivatePortal(Color colorToPropagate)
    {
        isActivated = true;  // Marcar este Portal como activado (ya no estar� disponible)

        // Quitar visualmente el Portal (destruir el objeto del juego)
        // Opcionalmente, podr�a ocultarse el sprite antes de destruir para efecto inmediato:
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // Remover referencia de la celda a este Portal, ya que se va a eliminar
        if (linkedCell != null)
        {
            linkedCell.linkedPortal = null;
        }

        // Eliminar este Portal de la lista de portales activos
        activePortals.Remove(this);

        // Generar propagaci�n del color en las celdas de los otros portales que siguen activos
        foreach (Portal otherPortal in new List<Portal>(activePortals))
        {
            // Solo propagar en portales que no hayan sido activados todav�a
            if (!otherPortal.isActivated && otherPortal.linkedCell != null)
            {
                // Iniciar la propagaci�n del color en la celda del otro Portal
                otherPortal.linkedCell.StartCoroutine(otherPortal.linkedCell.PropagateColorGradually(colorToPropagate));

            }
        }

        // Destruir el objeto Portal (remover del nivel)
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Remover de la lista en caso de que a�n est� (por seguridad)
        activePortals.Remove(this);
    }
}
