using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tactic
{
    public Task task;
    public float baseEvalPriorityScore;
    public float lastEvaluationTime;

    public float priority = 0f;

    public abstract void EvaluatePriority();

    public float GetEvaluationFrequencyScore()
    {
        return baseEvalPriorityScore - Time.time + lastEvaluationTime;
    }

    public abstract void Process();
}

public abstract class SquadTactic : Tactic
{
    public SquadManager squadManager;

    // squads doing the task or going to the task
    // squads will try to merge automatically if they are close enough
    public List<Squad> squads;

    public Vector2 position;

    // Returns null if the squad can't be shrinked anymore
    // The returned squad must be registed in the squadManager
    public abstract Squad TryShrink(List<Squad> totalSquads);

    public override void Process()
    {
        bool isCurrentTaskDoable = false;

        List<Squad> newTacticSquads = new List<Squad>(squads);

        List<Squad> allSquadsByDistance = new List<Squad>(squadManager.squads);
        // sort squads by distance to the task
        allSquadsByDistance.Sort((Squad a, Squad b) =>
        {
            // TODO : change into travel cost ? remove some points depending on if the squad is already doing a task ?
            float lengthA = (position - a.GetAveragePosition()).SqrMagnitude();
            float lengthB = (position - b.GetAveragePosition()).SqrMagnitude();

            return lengthA.CompareTo(lengthB);
        });

        // A squad that has been created after being shrinked
        Squad newSquad = new Squad();

        // search through all the squads, and will try to assign squads doing a tactic with less priority than the current one
        foreach (Squad squad in allSquadsByDistance)
        {
            // Wait for seconds
            if (squad.Tactic.priority < priority)
            {
                newTacticSquads.Add(squad);
                newSquad = TryShrink(newTacticSquads);
                if (newSquad != null)
                {
                    isCurrentTaskDoable = true;
                    break;
                }
            }
        }

        if (isCurrentTaskDoable)
        {
            foreach (Squad squad in newTacticSquads)
            {
                // Wait for seconds
                squad.Tactic = this;
            }
        }
        else
        {
            foreach (Squad squad in squads)
            {
                // Wait for seconds
                squad.Tactic = null;
            }
        }
    }
}