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


    //public List<Tactic> alltacticsByEvaluationPriority = new List<Tactic>();
    //public TaskRunner taskRunner = new TaskRunner();
    //public List<Tactic> allTacticsByPriority;

    //private void Update()
    //{
    //    if (taskRunner.IsRunningTask())
    //        taskRunner.UpdateCurrentTask();
    //    else
    //    {
    //        alltacticsByEvaluationPriority.Sort((Tactic a, Tactic b) => a.GetEvaluationFrequencyScore().CompareTo(b.GetEvaluationFrequencyScore()));

    //        for (int i = 0; i < alltacticsByEvaluationPriority.Count && alltacticsByEvaluationPriority[i].GetEvaluationFrequencyScore() < 0f; i++)
    //        {
    //            alltacticsByEvaluationPriority[i].EvaluatePriority();
    //            alltacticsByEvaluationPriority[i].lastEvaluationTime = Time.time;
    //            // wait for seconds
    //        }

    //        // Sort by priority
    //        allTacticsByPriority = new List<Tactic>(alltacticsByEvaluationPriority);
    //        allTacticsByPriority.Sort((Tactic a, Tactic b) => a.priority.CompareTo(b.priority));

    //        // Will try to assign enough units for priority tasks
    //        foreach (Tactic tactic in allTacticsByPriority)
    //        {
    //            tactic.Process();
    //        }

    //        // TODO : do things with idle squads ?
    //    }
    //}
}
