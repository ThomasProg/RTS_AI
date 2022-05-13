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

    public void QueryUnit(PointOfInterest pointOfInterest, StrategyAI.Blackboard blackboard)
    {
        if (pointOfInterest.priority < 0f)
        {
            pointOfInterest.RemoveAllSquads();
            // RunNextTask();
            return;
        }

        bool isCurrentTaskDoable = false;

        List<Squad> tacticNewSquads = new List<Squad>(pointOfInterest.squads);

        List<Squad> allSquadsByDistance = new List<Squad>(pointOfInterest.squadManager.squads);
        // sort squads by distance to the task
        allSquadsByDistance.Sort((Squad a, Squad b) =>
        {
            // TODO : change into travel cost ? remove some points depending on if the squad is already doing a task ?
            float lengthA = (pointOfInterest.position - a.GetAveragePosition()).SqrMagnitude();
            float lengthB = (pointOfInterest.position - b.GetAveragePosition()).SqrMagnitude();

            return lengthA.CompareTo(lengthB);
        });

        // A squad that has been created after being shrinked
        List<Squad> newSquads = new List<Squad>();

        // search through all the squads, and will try to assign squads doing a tactic with less priority than the current one
        foreach (Squad squad in allSquadsByDistance)
        {
            // Wait for seconds
            if (squad.PointOfInterest == null || squad.PointOfInterest.priority <= pointOfInterest.priority)
            {
                tacticNewSquads.Add(squad);
                if (pointOfInterest.TryShrink(ref tacticNewSquads))
                {
                    isCurrentTaskDoable = true;
                    break;
                }
            }
        }

        if (isCurrentTaskDoable)
        {
            foreach (Squad squad in tacticNewSquads)
            {
                // Wait for seconds
                squad.PointOfInterest = pointOfInterest;
            }
        }
        else
        {
            pointOfInterest.RemoveAllSquads();
        }

        // TODO : wait for seconds

        // RunNextTask();
    }
}