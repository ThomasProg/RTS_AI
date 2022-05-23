using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePointTask : IPOITask<StrategyAI.Blackboard>
{
    public CapturePointPoI capturePointPoI;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        foreach (Squad squad in capturePointPoI.squads)
        {
            if (squad.IsIdle)
                squad.GoCapturePoint(capturePointPoI.targetBuilding);
        }

        yield return null;
    }
}
