using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSquadTask : IPOITask<StrategyAI.Blackboard>
{
    public SquadPoI squadPoI;
    public float timeToLeadSquadToPoI = 1f;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        // Foreach makes an error, since reevaluation can be called during the wait of this loop
        // However, this task should not be continue, so it shouldn't be an error
        for (int i = 0; i < squadPoI.squads.Count; i++)
        {
            Squad squad = squadPoI.squads[i];
            if (squadPoI.enemySquad.Units.Count > 0 && squad.IsIdle)
            {
                //Debug.Log(Time.time + " : ========== GoCapturePoint : " + i);
                Unit unit = squadPoI.enemySquad.GetNearestUnit(squad.GetAveragePosition());
                
                squad.Goto(unit.transform.position, squad.GetSquadAttackRange());
                squad.AttackTarget(unit);
                yield return new WaitForSeconds(timeToLeadSquadToPoI);
            }
        }

        yield break;
    }
}
