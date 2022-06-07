using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PointOfInterest
{
    public float strengthRequiredAdditionalCoef = 1.5f; // 150%
    
    public float priority = 0f;
    public float strengthRequired = 0f;
    public SquadManager squadManager;
    public List<Squad> squads = new List<Squad>();
    public Vector2 position;

    public abstract void EvaluatePriority(StrategyAI.Blackboard blackboard);
    public abstract List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard);
    public virtual void AddSquad(Squad squad)
    {
        squads.Add(squad);
    }

    public virtual void RemoveSquad(Squad squad)
    {
        squads.Remove(squad);
    }

    public void RemoveAllSquads()
    {
        while (squads.Count > 0)
        {
            squads[0].PointOfInterest = null;
        }

        foreach (var pair in squadManager.newUnitsPoI)
        {
            List<PointOfInterest> pois = new List<PointOfInterest>();
            while (pair.Value.Count > 0)
            {
                pois.Add(pair.Value.Dequeue());
            }

            foreach (PointOfInterest poi in pois)
            {
                if (poi != this)
                {
                    pair.Value.Enqueue(poi);
                }
            }
        }
    }
}