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
        squad.OnSquadEmpty += UnregisterSquad;
        squads.Add(squad);

        foreach (Unit unit in squad.UnitList)
        {
            squadsOfUnits[unit] = squad;
        }
    }

    public void UnregisterSquad(Squad squad)
    {
        squad.OnSquadEmpty -= UnregisterSquad;
        squads.Remove(squad);

        foreach (Unit unit in squad.UnitList)
        {
            squadsOfUnits.Remove(unit);
        }
    }
}