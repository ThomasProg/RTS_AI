using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TryGetIdleGroups : PredicateTask
{
    StrategyAI.Blackboard blackboard;

    public override void OnStart()
    {
        blackboard = (StrategyAI.Blackboard)taskRunner.blackboard;

        blackboard.controller.StartCoroutine(StartAsync());
    }

    IEnumerator StartAsync()
    {
        yield return new WaitForSeconds(1.5f);

        blackboard.idleGroups = Squad.MakeSquadsDependingOnDistance(blackboard.allyUnits.FindAll((Unit unit) => unit.IsIdle));
        blackboard.squadManager.RegisterSquads(blackboard.idleGroups);

        RunNextTask();
    }

    protected override bool IsPredicateTrue()
    {
        return blackboard.idleGroups.Count > 0;
    }
}