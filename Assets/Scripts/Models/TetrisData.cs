using System.Collections.Generic;
using UnityEngine;

public enum TetrominoType 
{ 
    //палка
    I, 
    //кубик
    O,
    //буква Т
    T,
    // букви Г зеркальні
    J, L, 
    //молнії
    S, 
    Z,
}

public static class TetrisData
{
    // Stored as [Rotation Index 0-3][Block Index 0-3]
    // 0 = 0° (Spawn)
    // 1 = 90° Clockwise
    // 2 = 180°
    // 3 = 270° Clockwise (or 90° Counter-Clockwise)
    public static readonly Dictionary<TetrominoType, Vector2Int[][]> Shapes = new()
    {
        // I-Piece (Cyan) - Rotates inside a 4x4 grid
        [TetrominoType.I] = new Vector2Int[][]
        {
            new[] { new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 1,  0), new Vector2Int( 2,  0) }, // 0°
            new[] { new Vector2Int( 1,  1), new Vector2Int( 1,  0), new Vector2Int( 1, -1), new Vector2Int( 1, -2) }, // 90°
            new[] { new Vector2Int(-1, -1), new Vector2Int( 0, -1), new Vector2Int( 1, -1), new Vector2Int( 2, -1) }, // 180°
            new[] { new Vector2Int( 0,  1), new Vector2Int( 0,  0), new Vector2Int( 0, -1), new Vector2Int( 0, -2) }  // 270°
        },

        // J-Piece (Blue) - Left hook
        [TetrominoType.J] = new Vector2Int[][]
        {
            new[] { new Vector2Int(-1,  1), new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 1,  0) }, // 0°
            new[] { new Vector2Int( 1,  1), new Vector2Int( 0,  1), new Vector2Int( 0,  0), new Vector2Int( 0, -1) }, // 90°
            new[] { new Vector2Int( 1, -1), new Vector2Int( 1,  0), new Vector2Int( 0,  0), new Vector2Int(-1,  0) }, // 180°
            new[] { new Vector2Int(-1, -1), new Vector2Int( 0, -1), new Vector2Int( 0,  0), new Vector2Int( 0,  1) }  // 270°
        },

        // L-Piece (Orange) - Right hook
        [TetrominoType.L] = new Vector2Int[][]
        {
            new[] { new Vector2Int( 1,  1), new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 1,  0) }, // 0°
            new[] { new Vector2Int( 1, -1), new Vector2Int( 0,  1), new Vector2Int( 0,  0), new Vector2Int( 0, -1) }, // 90°
            new[] { new Vector2Int(-1, -1), new Vector2Int( 1,  0), new Vector2Int( 0,  0), new Vector2Int(-1,  0) }, // 180°
            new[] { new Vector2Int(-1,  1), new Vector2Int( 0, -1), new Vector2Int( 0,  0), new Vector2Int( 0,  1) }  // 270°
        },

        // O-Piece (Yellow) - Square, doesn't actually rotate
        [TetrominoType.O] = new Vector2Int[][]
        {
            new[] { new Vector2Int( 0,  0), new Vector2Int( 0,  1), new Vector2Int( 1,  1), new Vector2Int( 1,  0) }, // 0°
            new[] { new Vector2Int( 0,  0), new Vector2Int( 0,  1), new Vector2Int( 1,  1), new Vector2Int( 1,  0) }, // 90°
            new[] { new Vector2Int( 0,  0), new Vector2Int( 0,  1), new Vector2Int( 1,  1), new Vector2Int( 1,  0) }, // 180°
            new[] { new Vector2Int( 0,  0), new Vector2Int( 0,  1), new Vector2Int( 1,  1), new Vector2Int( 1,  0) }  // 270°
        },

        // S-Piece (Green) - Stairs curving right
        [TetrominoType.S] = new Vector2Int[][]
        {
            new[] { new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 0,  1), new Vector2Int( 1,  1) }, // 0°
            new[] { new Vector2Int( 0,  1), new Vector2Int( 0,  0), new Vector2Int( 1,  0), new Vector2Int( 1, -1) }, // 90°
            new[] { new Vector2Int( 1,  0), new Vector2Int( 0,  0), new Vector2Int( 0, -1), new Vector2Int(-1, -1) }, // 180°
            new[] { new Vector2Int( 0, -1), new Vector2Int( 0,  0), new Vector2Int(-1,  0), new Vector2Int(-1,  1) }  // 270°
        },

        // T-Piece (Purple) - T shape
        [TetrominoType.T] = new Vector2Int[][]
        {
            new[] { new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 1,  0), new Vector2Int( 0,  1) }, // 0°
            new[] { new Vector2Int( 0,  1), new Vector2Int( 0,  0), new Vector2Int( 0, -1), new Vector2Int( 1,  0) }, // 90°
            new[] { new Vector2Int( 1,  0), new Vector2Int( 0,  0), new Vector2Int(-1,  0), new Vector2Int( 0, -1) }, // 180°
            new[] { new Vector2Int( 0, -1), new Vector2Int( 0,  0), new Vector2Int( 0,  1), new Vector2Int(-1,  0) }  // 270°
        },

        // Z-Piece (Red) - Stairs curving left
        [TetrominoType.Z] = new Vector2Int[][]
        {
            new[] { new Vector2Int(-1,  1), new Vector2Int( 0,  1), new Vector2Int( 0,  0), new Vector2Int( 1,  0) }, // 0°
            new[] { new Vector2Int( 1,  1), new Vector2Int( 1,  0), new Vector2Int( 0,  0), new Vector2Int( 0, -1) }, // 90°
            new[] { new Vector2Int( 1, -1), new Vector2Int( 0, -1), new Vector2Int( 0,  0), new Vector2Int(-1,  0) }, // 180°
            new[] { new Vector2Int(-1, -1), new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 0,  1) }  // 270°
        }
    };
}