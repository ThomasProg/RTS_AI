using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PointOfInterest
{
    public float strengthRequiredAdditionalCoef = 1.5f; // 150%
    
    public float priority = 0f;
    public float strengthRequired;
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
        for (int i = squads.Count - 1; i > -1; i--)
        {
            squads[i].PointOfInterest = null;
        }
    }
}