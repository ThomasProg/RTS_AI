using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadCapturePoint : InBetweenTask
{
    StrategyAI.Blackboard blackboard;

    List<Squad> toRemove = new List<Squad>();

    public override void OnStart()
    {
        blackboard = (StrategyAI.Blackboard)taskRunner.blackboard;

        blackboard.controller.StartCoroutine(StartAsync());
    }

    float GetCapturePointTacticalScore(TargetBuilding targetBuilding, Vector3 pos)
    {
        return -Vector3.Distance(targetBuilding.transform.position, pos) / 1000f;
    }

    IEnumerator StartAsync()
    {
        yield return new WaitForSeconds(0.1f);

        Squad group = blackboard.idleGroups[0];
        //foreach (UnitGroup group in idleUnitGroups)
        {
            Vector3 pos = group.GetAveragePosition();

            SortedDictionary<float, TargetBuilding> capturePointsByPriority = new SortedDictionary<float, TargetBuilding>();
            float score = 0f;
            foreach (TargetBuilding capturePoint in blackboard.allCapturePoints)
            {
                if (capturePoint.GetTeam() == ETeam.Neutral)
                    score = 3f;
                else if (capturePoint.GetTeam() != blackboard.controller.GetTeam())
                {
                    score = 6f;
                }

                score += GetCapturePointTacticalScore(capturePoint, pos);

                capturePointsByPriority.Add(-score, capturePoint);
            }

            var e = capturePointsByPriority.GetEnumerator();
            e.MoveNext();
            TargetBuilding targetCapturePoint = e.Current.Value;

            Formation formation = new Formation();
            formation.units = group.units;

            toRemove.Clear();

            foreach (Unit unit in group.units)
            {
                unit.SetCaptureTarget(targetCapturePoint);
                unit.formation = formation;
            }
            toRemove.Add(group);
        }

        foreach (Squad g in toRemove)
        {
            blackboard.idleGroups.Remove(g);
        }

        RunNextTask();
    }
}