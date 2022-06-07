using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    public readonly HashSet<Squad> squads = new HashSet<Squad>();
    public readonly Dictionary<Unit, Squad> squadsOfUnits = new Dictionary<Unit, Squad>();
    public readonly Dictionary<Factory, Queue<PointOfInterest>> newUnitsPoI = new Dictionary<Factory, Queue<PointOfInterest>>();

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
    }

    // Returns true if a unit is being built for this factory
    public bool RequestUnit(Factory factory, int unitBuildID, PointOfInterest poi)
    {
        Queue<PointOfInterest> queue;
        if (!newUnitsPoI.TryGetValue(factory, out queue))
        {
            queue = new Queue<PointOfInterest>();
            newUnitsPoI.Add(factory, queue);
        }

        if (!queue.Contains(poi))
        {
            if (factory.RequestUnitBuild(unitBuildID))
            {
                queue.Enqueue(poi);
                return true;
            }
            else
                return false;
        }
        else
            return true;
    }

    public void LinkToAI(Factory factory)
    {
        factory.OnUnitBuiltPersistent += (Unit unit) =>
        {
            Squad newSquad = new Squad(unit);
            RegisterSquad(newSquad);
            Queue<PointOfInterest> poiQueue = newUnitsPoI[factory]; // should not crash
            if (poiQueue.Count > 0)
            {
                PointOfInterest newPoI = poiQueue.Dequeue();
                newSquad.PointOfInterest = newPoI;
            }
        };
    }
}