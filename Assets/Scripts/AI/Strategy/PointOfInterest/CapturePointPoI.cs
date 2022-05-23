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
        queryUnitsTask = new QueryUnitsTask() { pointOfInterest = this };
        capturePointTask = new CapturePointTask() { capturePointPoI = this };
    }

    public override void AddSquad(Squad squad)
    {
        base.AddSquad(squad);
        //squad.GoCapturePoint(targetBuilding);
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
            Statistic.EvaluateSquadsStrengthInZone(aiSquads, targetBuilding.GetInfluencePosition(), distPlayerUnitsToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (playerStrength > aiStrength)
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
            }
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
            
            // Add a priority depending on distance from the first factories
            if (playerController.Factories.Length > 0)
            {
                float sqrtDistToNearestFactory = float.MaxValue;
                foreach (Factory factory in playerController.Factories)
                {
                    sqrtDistToNearestFactory = Mathf.Min(sqrtDistToNearestFactory, (factory.GetInfluencePosition() - position).sqrMagnitude);
                }

                priority += 1f / Mathf.Sqrt(sqrtDistToNearestFactory);
            }
            priority += playerStrength == 0 ? 1 : aiStrength / playerStrength;
        }
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }

    void EvaluateDefendThisPointPriority(IEnumerable aiSquads)
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
            Statistic.EvaluateSquadsStrengthInZone(aiSquads, targetBuilding.GetInfluencePosition(), distPlayerUnitsToTarget);
        
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
            }
        }
        else
        {
            priority = 0f;
        }
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }
    
    QueryUnitsTask queryUnitsTask;
    CapturePointTask capturePointTask;

    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        queryUnitsTask.strengthRequired = 2 + priority;
        tasks.Add(queryUnitsTask);
        tasks.Add(capturePointTask);
        return tasks;
    }
}
