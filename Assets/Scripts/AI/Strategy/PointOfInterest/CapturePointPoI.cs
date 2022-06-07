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
    }

    public override void RemoveSquad(Squad squad)
    {
        base.RemoveSquad(squad);
    }

    public override void EvaluatePriority(StrategyAI.Blackboard blackboard)
    {
        PlayerController playerController = GameServices.GetPlayerController();
        AIController aiController = GameServices.GetAIController();
        Squad[] aiSquads = aiController.Squads;
        
        // These both functions are process following this steps:
        // - Evaluate depending on enemy squad distance witch enemy is opposite. Depending on AI personality
        // - Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        // - Apply coefficient to this result depending on distance from our base or enemy base. Depending on AI personality
        // - Apply direct coefficient depending on AI personality
        
        // If ai target a point that it don't own, evaluate priority to defend this point
        if (stratAI.controller.Team != targetBuilding.GetTeam())
        {
            EvaluateTakeThisPointPriority(aiSquads, playerController);
        }
        // If player can attack a near target without and can win it, evaluate priority to attack this point
        else if (stratAI.controller.Team == targetBuilding.GetTeam())
        {
            EvaluateDefendThisPointPriority(aiSquads);
        }
    }
    
    void EvaluateTakeThisPointPriority(IEnumerable aiSquads, PlayerController playerController)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<GameUtility.POITargetByEnemySquad> playerSquadObjectives = 
            GameUtility.GetPOITargetByEnemySquad(this, GameServices.GetAIController(), GameServices.GetPlayerController(), 1f + stratAI.subjectiveUtilitySystem.GetStat("InformationNeed").Value,  0.4f);

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
            GameUtility.EvaluateSquadsStrengthInZone(aiSquads, targetBuilding.GetInfluencePosition(), distPlayerUnitsToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            priority = 0;

        }
        else
        {
            if (targetBuilding.GetTeam() == ETeam.Neutral)
            {
                priority = 3f;
            }
            else // enemy team
            {
                priority = 6f;
            }
            
            // Add a priority depending on distance from the factories
            if (stratAI.controller.Factories.Length > 0)
            {
                float sqrtDistToNearestFactory = float.MaxValue;
                foreach (Factory factory in stratAI.controller.Factories)
                {
                    sqrtDistToNearestFactory = Mathf.Min(sqrtDistToNearestFactory, (factory.GetInfluencePosition() - position).sqrMagnitude);
                }

                priority += 1f / Mathf.Sqrt(sqrtDistToNearestFactory);
                
                if (targetBuilding.GetTeam() == ETeam.Neutral)
                {
                    priority *= stratAI.subjectiveUtilitySystem.GetUtility("Capture").Value;
                }
                else // enemy team
                {
                    priority *= stratAI.subjectiveUtilitySystem.GetUtility("Attack").Value;
                }
            }
        }

        strengthRequired = Mathf.Max(1, playerStrength * strengthRequiredAdditionalCoef);
    }

    void EvaluateDefendThisPointPriority(IEnumerable aiSquads)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<GameUtility.POITargetByEnemySquad> playerSquadObjectives = 
            GameUtility.GetPOITargetByEnemySquad(this, GameServices.GetAIController(), GameServices.GetPlayerController(), 1f +  stratAI.subjectiveUtilitySystem.GetStat("InformationNeed").Value,  0.4f);

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
            GameUtility.EvaluateSquadsStrengthInZone(aiSquads, targetBuilding.GetInfluencePosition(), distPlayerUnitsToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
        {
            if (targetBuilding.GetTeam() == ETeam.Neutral)
            {
                priority = 3f;
            }
            else // enemy ally
            {
                priority = 6f;
            }
            
            // Add a priority depending on distance from the factories
            if (stratAI.controller.Factories.Length > 0)
            {
                float sqrtDistToNearestFactory = float.MaxValue;
                foreach (Factory factory in stratAI.controller.Factories)
                {
                    sqrtDistToNearestFactory = Mathf.Min(sqrtDistToNearestFactory, (factory.GetInfluencePosition() - position).sqrMagnitude);
                }

                priority += 1f / Mathf.Sqrt(sqrtDistToNearestFactory);
                priority *= stratAI.subjectiveUtilitySystem.GetUtility("Protect").Value;
            }
        }
        else
        {
            priority = 0f;
        }
        
        strengthRequired = playerStrength * strengthRequiredAdditionalCoef;
    }

    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        tasks.Add(new QueryUnitsTask() { pointOfInterest = this, strengthRequired = Mathf.Ceil(strengthRequired) });
        tasks.Add(new CapturePointTask() { capturePointPoI = this });
        return tasks;
    }
}
