using UnityEngine;

public class WingFormation : Formation
{
    public WingFormation(Squad squad) : base(squad)
    {
    }

    protected override void UpdateRelativePositions()
    {
        relativePositions.Clear();
        formationBarycenter = Vector3.zero;

        Vector3 unitPos = Vector3.zero;
        relativePositions.Add(unitPos);
        formationBarycenter += unitPos;

        for (int i = 1; i < unitCount; i++)
        {
            if (i % 2 == 0)
                unitPos = i * 0.5f * new Vector3(1, 0, 1);
            else
                unitPos = (i + 1) * 0.5f * new Vector3(-1, 0, 1);
            
            relativePositions.Add(unitPos);
            formationBarycenter += unitPos;
        }

        formationBarycenter /= unitCount;
    }
}