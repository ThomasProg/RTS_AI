using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TryGetIdleGroups : PredicateTask
{
    StrategyAI.Blackboard blackboard;

    public List<UnitGroup> idleGroups;

    public override void OnStart()
    {
        blackboard = (StrategyAI.Blackboard)taskRunner.blackboard;

        blackboard.controller.StartCoroutine(StartAsync());
    }

    IEnumerator StartAsync()
    {
        yield return new WaitForSeconds(0.5f);

        idleGroups = UnitGroup.MakeGroupsDependingOnDistance(blackboard.allyUnits.FindAll((Unit unit) => unit.IsIdle));

        RunNextTask();
    }

    protected override bool IsPredicateTrue()
    {
        return idleGroups.Count > 0;
    }
}