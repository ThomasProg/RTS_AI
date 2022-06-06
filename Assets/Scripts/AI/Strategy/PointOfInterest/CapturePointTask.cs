using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePointTask : IPOITask<StrategyAI.Blackboard>
{
    public CapturePointPoI capturePointPoI;
    public float timeToLeadSquadToPoI = 1f;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        // Foreach makes an error, since reevaluation can be called during the wait of this loop
        // However, this task should not be continue, so it shouldn't be an error
        for (int i = 0; i < capturePointPoI.squads.Count; i++)
        {
            Squad squad = capturePointPoI.squads[i];
            if (squad.IsIdle)
            {
                //Debug.Log(Time.time + " : ========== GoCapturePoint : " + i);
                squad.GoCapturePoint(capturePointPoI.targetBuilding);
                yield return new WaitForSeconds(timeToLeadSquadToPoI);
            }
        }

        yield break;
    }
}
