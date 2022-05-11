using UnityEngine;

public class CircleFormation : Formation
{
    public CircleFormation(Squad squad) : base(squad)
    {
    }

    protected override void UpdateRelativePositions()
    {
        relativePositions.Clear();
        formationBarycenter = Vector3.zero;

        for (int i = 0; i < unitCount; i++)
        {
            Vector3 unitPos = Vector3.right * Scale;
            float rotationDelta = i / (float) unitCount;
            unitPos = Quaternion.Euler(0f, rotationDelta * 360, 0f) * unitPos;
            
            relativePositions.Add(unitPos);
            formationBarycenter += unitPos;
        }

        formationBarycenter /= unitCount;
    }
}