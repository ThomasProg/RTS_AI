using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CapturePointPoI : PointOfInterest
{
    public StrategyAI stratAI;
    public TargetBuilding targetBuilding;

    public CapturePointPoI(TargetBuilding targetBuilding)
    {
        this.targetBuilding = targetBuilding;
        position = targetBuilding.GetInfluencePosition();
    }

    public override void AddSquad(Squad squad)
    {
        base.AddSquad(squad);
        squad.GoCapturePoint(targetBuilding);
    }

    public override void RemoveSquad(Squad squad)
    {
        base.RemoveSquad(squad);
    }

    public override void EvaluatePriority(StrategyAI.Blackboard blackboard)
    {
        // todo : change priority if enemies are close, etc
        if (targetBuilding.GetTeam() == stratAI.controller.GetTeam())
        {
            priority = -1f;
        }
        else if (targetBuilding.GetTeam() == ETeam.Neutral)
        {
            priority = 3f;
        }
        else // enemy team
        {
            priority = 6f;
        }

        priority += 1f / (stratAI.controller.Factories[0].GetInfluencePosition() - position).SqrMagnitude();
    }

    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        tasks.Add(new QueryUnitsTask() { pointOfInterest = this });
        //tasks.Add(new CapturePointTask() { capturePointPoI = this });
        return tasks;
    }

    public override bool TryShrink(ref List<Squad> totalSquads)
    {
        if (totalSquads.Count <= 0)
            return false;

        List<Squad> newTotalSquads = new List<Squad>();

        //Squad newSquad = null;

        int nbRequiredUnits = 5;

        int nbCurrentUnits = 0;
        foreach (Squad squad in totalSquads)
        {
            nbCurrentUnits += squad.UnitList.Count;
            newTotalSquads.Add(squad);
            if (nbCurrentUnits >= nbRequiredUnits)
            {
                int nbUnitsToRemove = nbCurrentUnits - nbRequiredUnits;
                if (nbUnitsToRemove == squad.UnitList.Count)
                {
                    newTotalSquads.Remove(squad);
                }
                else
                {
                    // TODO : Split
                    //newSquad = squad.Split(nbUnitsToRemove);
                }
                nbCurrentUnits -= nbUnitsToRemove;
                break;
            }
        }

        totalSquads = newTotalSquads;

        return nbRequiredUnits <= nbCurrentUnits;
    }
}
