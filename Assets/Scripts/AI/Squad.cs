using System.Collections;
using System.Collections.Generic;
using InfluenceMapPackage;
using UnityEngine;

public class Squad : IInfluencer
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

    public float GetStrength()
    {
        float strength = 0;
        foreach (Unit unit in units)
        {
            strength += unit.Cost;
        }

        return strength;
    }
    
    public float GetSqrInfluenceRadius()
    {
        float sqrInfluenceRadius = 0;
        Vector2 squadPos = GetAveragePosition();
        
        foreach (Unit unit in units)
        {
            float unitInflRad = unit.GetInfluenceRadius();
            sqrInfluenceRadius = Mathf.Max(sqrInfluenceRadius, (squadPos - unit.GetInfluencePosition()).sqrMagnitude + unitInflRad * unitInflRad);
        }

        return sqrInfluenceRadius;
    }
    
    public Vector2 GetInfluencePosition()
    {
        return GetAveragePosition();
    }

    public float GetInfluenceRadius()
    {
        return GetSqrInfluenceRadius();
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
