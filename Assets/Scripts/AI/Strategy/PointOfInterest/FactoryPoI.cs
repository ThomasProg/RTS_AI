using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FactoryPoI : PointOfInterest
{
    public StrategyAI stratAI;
    public Factory factory;

    public FactoryPoI(Factory factory)
    {
        this.factory = factory;
        position = factory.GetInfluencePosition();
        queryUnitsTask = new QueryUnitsTask() { pointOfInterest = this };
    }

    public override void AddSquad(Squad squad)
    {
        base.AddSquad(squad);
        //squad.GoCapturePoint(factory);
    }

    public override void RemoveSquad(Squad squad)
    {
        base.RemoveSquad(squad);
    }

    public override void EvaluatePriority(StrategyAI.Blackboard blackboard)
    {
        PlayerController playerController = GameServices.GetPlayerController();
        AIController aiController = GameServices.GetAIController();
        Squad[] playerSquads = playerController.Squads;
        Squad[] aiSquads = aiController.Squads;
        
        // These both functions are process following this steps:
        // - Evaluate depending on enemy squad distance witch enemy is opposite. Depending on AI personality
        // - Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        // - Apply coefficient to this result depending on distance from our base or enemy base. Depending on AI personality
        // - Apply direct coefficient depending on AI personality
        
        // If ai target a point that it don't own, evaluate priority to defend this point
        if (stratAI.controller.Team != factory.GetTeam())
        {
            EvaluateTakeThisPointPriority(aiSquads, playerController);
        }
        // If player can attack a near target without and can win it, evaluate priority to attack this point
        else if (stratAI.controller.Team == factory.GetTeam())
        {
            EvaluatedefendThisPointPriority(aiSquads, playerSquads, playerController);
        }
    }
    
    void EvaluateTakeThisPointPriority(IEnumerable aiSquads, PlayerController playerController)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<Statistic.POITargetByEnemySquad> playerSquadObjectives = 
            Statistic.GetPOITargetByEnemySquad(this, GameServices.GetPlayerController().GetTeam() , 10f, 1.1f, 0.5f);

        float distPlayerUnitsToTarget = float.MinValue;
        float playerStrength = 0f;
        foreach (Statistic.POITargetByEnemySquad playerSquadObjective in playerSquadObjectives)
        {
            float sqrDistSquadTarget =
                (playerSquadObjective.enemy.GetAveragePosition() - playerSquadObjective.poi.position)
                .sqrMagnitude;
            if (distPlayerUnitsToTarget < sqrDistSquadTarget)
            {
                distPlayerUnitsToTarget = sqrDistSquadTarget;
            }
            
            playerStrength += playerSquadObjective.enemy.GetStrength();
        }

        float aiStrength =
            Statistic.EvaluateSquadsStrengthInZone(aiSquads, factory.GetInfluencePosition(), distPlayerUnitsToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            priority = 0f;
        }
        else
        {
            priority = 6f;
        }
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }

    void EvaluatedefendThisPointPriority(IEnumerable enemySquad, IEnumerable playerSquads, PlayerController playerController)
    {
        bool squadFound = false;
        foreach (Squad squad in playerSquads)
        {
            // TODO: Check if squad is occuped with highter priority task
            
            float squadToTargetSqrDist = (factory.GetInfluencePosition() - squad.GetAveragePosition()).sqrMagnitude;
            float playerSquadStrength = Statistic.EvaluateSquadsStrengthInZone(new []{squad}, factory.GetInfluencePosition(), squadToTargetSqrDist);
            float aiStrength = Statistic.EvaluateSquadsStrengthInZone(enemySquad, factory.GetInfluencePosition(), squadToTargetSqrDist);

            if (playerSquadStrength > aiStrength)
            {
                priority = 6f;
                squadFound = true;
                break;
            }
        }
        
        if (!squadFound)
            priority = 0f;
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }

    QueryUnitsTask queryUnitsTask;

    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        queryUnitsTask.strengthRequired = 2 + priority;
        tasks.Add(queryUnitsTask);
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
