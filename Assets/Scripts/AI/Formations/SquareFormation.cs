using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SquareFormation : Formation
{
    public float Ratio = 1.5f;

    /// <summary>
    /// Create a Square Formation
    /// </summary>
    /// <param name="squad">The squad this formation is attached to</param>
    /// <param name="squareRatio">The preferred Height by Width ratio of the square formation</param>
    public SquareFormation(Squad squad, float squareRatio) : base(squad)
    {
        Ratio = squareRatio;
    }

    protected override void UpdateRelativePositions()
    {
        relativePositions.Clear();
        formationBarycenter = Vector3.zero;

        int unitPerRow = Mathf.FloorToInt(Mathf.Sqrt(unitCount * Ratio));

        for (int i = 0; i < unitCount; i++)
        {
            Vector3 unitPos = new Vector3(i % unitPerRow, 0, Mathf.FloorToInt((float) i / unitPerRow));
            relativePositions.Add(unitPos);
            formationBarycenter += unitPos;
        }

        formationBarycenter /= unitCount;
    }
}