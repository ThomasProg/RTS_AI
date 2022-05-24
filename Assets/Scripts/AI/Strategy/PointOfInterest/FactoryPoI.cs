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
        
        if (stratAI.controller.Team != factory.GetTeam())
        {
            EvaluateTakeThisPointPriority(aiSquads);
        }
        else if (stratAI.controller.Team == factory.GetTeam())
        {
            EvaluateDefendThisPointPriority(aiSquads);
        }
    }
    
    void EvaluateTakeThisPointPriority(IEnumerable aiSquads)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<GameUtility.POITargetByEnemySquad> playerSquadObjectives = 
            GameUtility.GetPOITargetByEnemySquad(this, GameServices.GetPlayerController().GetTeam() , 10f, 1.1f, 0.5f);

        float distPlayerUnitsToTarget = float.MinValue;
        float playerStrength = 0f;
        foreach (GameUtility.POITargetByEnemySquad playerSquadObjective in playerSquadObjectives)
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
            GameUtility.EvaluateSquadsStrengthInZone(aiSquads, factory.GetInfluencePosition(), distPlayerUnitsToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            priority = 0f;
        }
        else
        {
            priority = 6f;
            priority += playerStrength == 0 ? 1 : aiStrength / playerStrength;
        }
        
        strengthRequired = playerStrength * strengthRequiredAdditionalCoef;
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }


    void EvaluateDefendThisPointPriority(IEnumerable aiSquads)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<GameUtility.POITargetByEnemySquad> playerSquadObjectives = 
            GameUtility.GetPOITargetByEnemySquad(this, GameServices.GetPlayerController().GetTeam() , 10f, 1.1f, 0.5f);

        float distPlayerUnitsToTarget = float.MinValue;
        float playerStrength = 0f;
        foreach (GameUtility.POITargetByEnemySquad playerSquadObjective in playerSquadObjectives)
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
            GameUtility.EvaluateSquadsStrengthInZone(aiSquads, factory.GetInfluencePosition(), distPlayerUnitsToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            priority = 6f;
        }
        else
        {
            priority = 0f;
        }

        strengthRequired = playerStrength * strengthRequiredAdditionalCoef;
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }

    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        tasks.Add(new QueryUnitsTask() { pointOfInterest = this, strengthRequired = Mathf.Ceil(Mathf.Max(strengthRequired, 1f)) }); // Strength : [1.. strengthRequired + 1]
        tasks.Add(new DestroyFactoryTask() { factoryPoI = this });
        return tasks;
    }
}
