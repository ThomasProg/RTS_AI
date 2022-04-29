using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Squad
{
    public List<Unit> units;

    public Vector3 GetAveragePosition()
    {
        Vector3 averagePosition = Vector3.zero;
        foreach (Unit unit in units)
        {
            averagePosition += unit.transform.position;
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
                if (Vector3.Distance(squads[i].GetAveragePosition(), squads[j].GetAveragePosition()) < dist)
                {
                    squads[i].units.AddRange(squads[j].units);
                    squads.RemoveAt(j);
                    i--;
                    break;
                }
            }
        }
    }

    public static List<Squad> MakeSquadsDependingOnDistance(List<Unit> units, float dist = 50)
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
}
