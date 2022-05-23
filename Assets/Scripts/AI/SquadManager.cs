using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    public readonly HashSet<Squad> squads = new HashSet<Squad>();
    public readonly Dictionary<Unit, Squad> squadsOfUnits = new Dictionary<Unit, Squad>();

    public void RegisterSquads(IEnumerable<Squad> newSquads)
    {
        foreach (Squad squad in newSquads)
        {
            RegisterSquad(squad);
        }
    }

    public void RegisterSquad(Squad squad)
    {
        squads.Add(squad);
        foreach (Unit unit in squad.UnitList)
        {
            squadsOfUnits.Add(unit, squad);
        }
    }

    public void UnregisterSquad(Squad squad)
    {
        squads.Remove(squad);
        foreach (Unit unit in squad.UnitList)
        {
            squadsOfUnits.Remove(unit);
        }
    }

    public bool IsSquadGoingToCapturePoint(TargetBuilding capturePoint)
    {
        foreach (Squad squad in squads)
        {
            if (squad.IsGoingToCapturePoint(capturePoint))
            {
                return true;
            }
        }

        return false;
    }
}