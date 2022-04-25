using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class HasEnoughUnits : PredicateTask
{
    StrategyAI.Blackboard blackboard;

    public override void OnStart()
    {
        blackboard = (StrategyAI.Blackboard)taskRunner.blackboard;
        RunNextTask();
    }

    int GetLimit()
    {
        return blackboard.nbEnemyUnits - 2;
    }

    protected override bool IsPredicateTrue()
    {
        return blackboard.nbUnits > GetLimit();
    }
}