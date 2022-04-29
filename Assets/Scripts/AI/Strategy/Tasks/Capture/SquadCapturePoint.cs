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

    float GetCapturePointTacticalScore(TargetBuilding targetBuilding, Vector2 pos)
    {
        return -Vector2.Distance(targetBuilding.Get2DPosition(), pos) / 30f;
    }

    IEnumerator StartAsync()
    {
        yield return new WaitForSeconds(0.1f);

        Squad squad = blackboard.idleGroups[0];
        //foreach (UnitGroup squad in idleUnitGroups)
        {
            Vector2 pos = squad.GetAveragePosition();

            SortedDictionary<float, TargetBuilding> capturePointsByPriority = new SortedDictionary<float, TargetBuilding>();
            foreach (TargetBuilding capturePoint in blackboard.allCapturePoints)
            {
                float score = 0f;
                if (blackboard.squadManager.IsSquadGoingToCapturePoint(capturePoint))
                    score -= 6f;

                if (capturePoint.GetTeam() == ETeam.Neutral)
                    score += 3f;
                else if (capturePoint.GetTeam() != blackboard.controller.GetTeam())
                {
                    score += 6f;
                }

                score += GetCapturePointTacticalScore(capturePoint, pos);

                capturePointsByPriority.Add(-score, capturePoint);
            }

            var e = capturePointsByPriority.GetEnumerator();
            e.MoveNext();
            TargetBuilding targetCapturePoint = e.Current.Value;

            if (targetCapturePoint != null)
            {
                toRemove.Clear();

                squad.GoCapturePoint(targetCapturePoint);

                toRemove.Add(squad);
            }
        }

        foreach (Squad g in toRemove)
        {
            blackboard.idleGroups.Remove(g);
        }

        RunNextTask();
    }
}