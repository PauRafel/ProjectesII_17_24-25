using UnityEngine;

public class VelocityCells : MonoBehaviour
{
    private bool isSpeedIncreased = false;
    public static float propagationDelay = 0.1f; 
    private const float normalSpeed = 0.1f;
    private const float increasedSpeed = 0.05f; 

    public void MorePropagationSpeed()
    {
        isSpeedIncreased = !isSpeedIncreased; // Alternar entre true y false
        propagationDelay = isSpeedIncreased ? increasedSpeed : normalSpeed; // Cambiar la velocidad
    }
}