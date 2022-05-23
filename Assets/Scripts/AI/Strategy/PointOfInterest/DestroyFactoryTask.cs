using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DestroyFactoryTask : IPOITask<StrategyAI.Blackboard>
{
    public FactoryPoI factoryPoI;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        foreach (Squad squad in factoryPoI.squads)
        {
            squad.AttackTarget(factoryPoI.factory);
        }

        yield return null;
    }
}