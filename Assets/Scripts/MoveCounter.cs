using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCounter : MonoBehaviour
{
    private int movesUsed = 0;

    public void IncrementMoves()
    {
        movesUsed++;
    }

    public int GetMovesUsed()
    {
        return movesUsed;
    }
}
