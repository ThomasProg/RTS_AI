using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SquadPoI : PointOfInterest
{
    public StrategyAI stratAI;
    public Squad enemySquad;

    public SquadPoI(Squad enemySquad)
    {
        this.enemySquad = enemySquad;
        position = enemySquad.GetInfluencePosition();
        queryUnitsTask = new QueryUnitsTask() { pointOfInterest = this };
    }

    public override void AddSquad(Squad squad)
    {
        base.AddSquad(squad);
    }

    public override void RemoveSquad(Squad squad)
    {
        base.RemoveSquad(squad);
    }

    public override void EvaluatePriority(StrategyAI.Blackboard blackboard)
    {
        AIController aiController = GameServices.GetAIController();
        Squad[] aiSquads = aiController.Squads;
        
        // These both functions are process following this steps:
        // - Evaluate depending on enemy squad distance witch enemy is opposite. Depending on AI personality
        // - Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        // - Apply coefficient to this result depending on distance from our base or enemy base. Depending on AI personality
        // - Apply direct coefficient depending on AI personality
        
        if (stratAI.controller.Team != enemySquad.GetTeam())
        {
            EvaluateAttackSquadPriority(aiSquads);
        }
        else if (stratAI.controller.Team == enemySquad.GetTeam())
        {
            EvaluateDefendThisPointPriority(aiSquads);
        }
    }

    void EvaluateAttackSquadPriority(IEnumerable aiSquads)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<GameUtility.POITargetByEnemySquad> playerSquadObjectives = 
            GameUtility.GetPOITargetByEnemySquad(this, GameServices.GetAIController(), GameServices.GetPlayerController(),1f + stratAI.subjectiveUtilitySystem.GetStat("InformationNeed").Value , 0.4f);

        float distPlayerUnitsToTarget = float.MinValue;
        float playerStrength = enemySquad.GetStrength();
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
            GameUtility.EvaluateSquadsStrengthInZone(aiSquads, enemySquad.GetInfluencePosition(), distPlayerUnitsToTarget) * stratAI.subjectiveUtilitySystem.GetStat("Patience").OneNegValue * 2f;
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            priority = 0f;
        }
        else
        {
            priority = 6f;
            priority += playerStrength == 0 ? 1 : aiStrength / playerStrength;
            priority *= stratAI.subjectiveUtilitySystem.GetUtility("Attack").Value;
        }
        
        strengthRequired = playerStrength * strengthRequiredAdditionalCoef;
    }


    void EvaluateDefendThisPointPriority(IEnumerable aiSquads)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<GameUtility.POITargetByEnemySquad> playerSquadObjectives = 
            GameUtility.GetPOITargetByEnemySquad(this, GameServices.GetAIController(), GameServices.GetPlayerController(), 1f + stratAI.subjectiveUtilitySystem.GetStat("InformationNeed").Value, 0.4f);

        float distPlayerUnitsToTarget = float.MinValue;
        float playerStrength = enemySquad.GetStrength();
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
            GameUtility.EvaluateSquadsStrengthInZone(aiSquads, enemySquad.GetInfluencePosition(), distPlayerUnitsToTarget) * stratAI.subjectiveUtilitySystem.GetStat("Patience").OneNegValue * 2f;
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            priority = 6f;
            priority *= stratAI.subjectiveUtilitySystem.GetUtility("Protect").Value;
        }
        else
        {
            priority = 0f;
        }
        
        strengthRequired = playerStrength * strengthRequiredAdditionalCoef;
    }

    QueryUnitsTask queryUnitsTask;

    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        queryUnitsTask.strengthRequired = Mathf.Ceil(Mathf.Max(strengthRequired, 1f)); // [1.. strengthRequired + 1]
        tasks.Add(queryUnitsTask);
        tasks.Add(new AttackSquadTask() { squadPoI = this });
        return tasks;
    }
}
