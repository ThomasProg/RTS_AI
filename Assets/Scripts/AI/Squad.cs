using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Squad
{
    public List<Unit> units;
    Formation formation = new Formation();
    TargetBuilding targetCapturePoint;

    public Vector2 GetAveragePosition()
    {
        Vector2 averagePosition = Vector2.zero;
        foreach (Unit unit in units)
        {
            averagePosition += unit.GetInfluencePosition();
        }
        return averagePosition / units.Count;
    }

    // Modifiy in place
    public static void FuseSquadsDependingOnDistance(List<Squad> squads, float dist = 50)
    {
        for (int i = 0; i < squads.Count; i++)
        {
            for (int j = i + 1; j < squads.Count; j++)
            {
                if ((squads[i].GetAveragePosition() - squads[j].GetAveragePosition()).sqrMagnitude < dist * dist)
                {
                    squads[i].units.AddRange(squads[j].units);
                    squads.RemoveAt(j);
                    i--;
                    break;
                }
            }
        }
    }

    public static List<Squad> MakeSquadsDependingOnDistance(IEnumerable<Unit> units, float dist = 50)
    {
        List<Squad> squads = new List<Squad>();
        foreach (Unit unit in units)
        {
            Squad squad = new Squad { units = new List<Unit>() };
            squad.units.Add(unit);
            squads.Add(squad);
        }

        FuseSquadsDependingOnDistance(squads, dist);
        return squads;
    }

    public void GoCapturePoint(TargetBuilding targetCapturePoint)
    {
        foreach (Unit unit in units)
        {
            unit.SetCaptureTarget(targetCapturePoint);
        }

        this.targetCapturePoint = targetCapturePoint;
    }

    public bool IsGoingToCapturePoint(TargetBuilding capturePoint)
    {
        return targetCapturePoint == capturePoint;
    }
}
