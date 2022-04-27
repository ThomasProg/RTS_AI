using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class HasEnoughBuildings : PredicateTask
{
    StrategyAI.Blackboard blackboard;

    public override void OnStart()
    {
        blackboard = (StrategyAI.Blackboard)taskRunner.blackboard;
        RunNextTask();
    }

    int GetLimit()
    {
        return blackboard.nbEnemyBuildings - 1;
    }

    protected override bool IsPredicateTrue()
    {
        return blackboard.nbBuildings > GetLimit();
    }
}
