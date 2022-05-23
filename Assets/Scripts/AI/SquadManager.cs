using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    public HashSet<Squad> squads = new HashSet<Squad>();

    public void RegisterSquads(IEnumerable<Squad> newSquads)
    {
        foreach (Squad squad in newSquads)
        {
            squads.Add(squad);
        }
    }

    public void RegisterSquad(Squad squad)
    {
        squads.Add(squad);
    }

    public void UnregisterSquad(Squad squad)
    {
        squads.Remove(squad);
    }
}