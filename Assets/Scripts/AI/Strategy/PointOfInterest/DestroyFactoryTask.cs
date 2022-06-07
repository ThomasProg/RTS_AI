using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DestroyFactoryTask : IPOITask<StrategyAI.Blackboard>
{
    public FactoryPoI factoryPoI;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        //foreach (Squad squad in factoryPoI.squads)
        for (int i = 0; i < factoryPoI.squads.Count; i++)
        {
            Squad squad = factoryPoI.squads[i];
            if (squad.IsIdle && factoryPoI != null && factoryPoI.factory != null)
            {
                squad.Regroup();
                squad.Goto(factoryPoI.factory.transform.position, squad.GetSquadAttackRange());
                squad.AttackTarget(factoryPoI.factory);
            }
        }

        yield break;
    }
}