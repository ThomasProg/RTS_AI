using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructFactoryTask : IPOITask<StrategyAI.Blackboard>
{
    public Vector2 position;
    public int idFactoryToBuild;
    public Factory master;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        GameServices.GetAIController().RequestFactoryBuild(master, idFactoryToBuild, GameUtility.ToVec3(position));
        yield break;
    }
}
