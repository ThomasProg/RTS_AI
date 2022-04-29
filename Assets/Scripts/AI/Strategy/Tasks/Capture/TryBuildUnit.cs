using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TryBuildUnit : PredicateTask
{
    StrategyAI.Blackboard blackboard;

    public List<Squad> idleGroups;
    bool isSuccess = false;

    public override void OnStart()
    {
        blackboard = (StrategyAI.Blackboard)taskRunner.blackboard;

        blackboard.controller.StartCoroutine(StartAsync());
    }

    IEnumerator StartAsync()
    {
        if (blackboard.allyFactories.Count <= 0)
        {
            RunNextTask();
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        bool isUnitBuilding;
        do
        {

            isUnitBuilding = blackboard.allyFactories[0].RequestUnitBuild(0);
            if (isUnitBuilding)
            {
                yield return new WaitForSeconds(0.14f);
                isSuccess = true;
            }
        } while (isUnitBuilding);

        RunNextTask();
    }

    protected override bool IsPredicateTrue()
    {
        return isSuccess;
    }
}