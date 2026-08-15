using System.Collections.Generic;
using UnityEngine;

public class PlacedPiece
{
    public TetrominoType Type;
    public int RotationIndex;
    public Vector3Int PivotPosition;
    public List<Vector3Int> OccupiedCells = new();
}