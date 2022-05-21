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
            EvaluateDefendingPriority(playerSquads);
        }
        // If player can attack a near target without and can win it, evaluate priority to attack this point
        else if (stratAI.controller.Team == targetBuilding.GetTeam())
        {
            EvaluateAttackingPriority(aiSquads, playerSquads);
        }
    }

    void EvaluateAttackingPriority(IEnumerable enemySquad, IEnumerable playerSquads)
    {
        bool squadFound = false;
        foreach (Squad squad in playerSquads)
        {
            // TODO: Check if squad is occuped with highter priority task
            
            float squadToTargetSqrDist = (targetBuilding.GetInfluencePosition() - squad.GetAveragePosition()).sqrMagnitude;
            float playerSquadStrength = Statistic.EvaluateSquadsStrengthInZone(new []{squad}, targetBuilding.GetInfluencePosition(), squadToTargetSqrDist);
            float aiStrength = Statistic.EvaluateSquadsStrengthInZone(enemySquad, targetBuilding.GetInfluencePosition(), squadToTargetSqrDist);

            if (playerSquadStrength > aiStrength)
            {
                if (targetBuilding.GetTeam() == ETeam.Neutral)
                {
                    priority = 3f;
                }
                else // enemy team
                {
                    priority = 6f;
                }
            
                // Add a priority depending on distance from the first factory (AI need a safe place in priority)
                priority += 1f / (GameServices.GetPlayerController().Factories[0].GetInfluencePosition() - position).SqrMagnitude();
                squadFound = true;
                break;
            }
        }
        
        if (!squadFound)
            priority = 0f;
        
        // Apply direct coefficient depending on AI personality
        // TODO:
    }
    
    void EvaluateDefendingPriority(IEnumerable playerSquads)
    {
        // Get all enemy squads should attack this point
        // Accept enemy squad only if probability is upper than X % (based on 50% +/- AI personality)
        List<Statistic.POITargetByEnemySquad> enemySquadObjectives = 
            Statistic.GetPOITargetByEnemySquad(this, stratAI.controller.Team, 10f, 1.1f, 0.5f);

        float distEnemiesToTarget = float.MinValue;
        float enemyStrength = 0f;
        foreach (Statistic.POITargetByEnemySquad enemySquadObjective in enemySquadObjectives)
        {
            float sqrDistSquadTarget =
                (enemySquadObjective.enemy.GetAveragePosition() - enemySquadObjective.poi.position)
                .sqrMagnitude;
            if (distEnemiesToTarget < sqrDistSquadTarget)
            {
                distEnemiesToTarget = sqrDistSquadTarget;
            }
            
            enemyStrength += enemySquadObjective.enemy.GetStrength();
        }

        float playerStrength =
            Statistic.EvaluateSquadsStrengthInZone(playerSquads, targetBuilding.GetInfluencePosition(), distEnemiesToTarget);
        
        // Process the balance of power and evaluate the cost of loose/keep this point. Depending on AI personality
        if (enemyStrength > playerStrength)
        {
            if (targetBuilding.GetTeam() == ETeam.Neutral)
            {
                priority = 3f;
            }
            else // enemy team
            {
                priority = 6f;
            }
            
            // Add a priority depending on distance from the first factory (AI need a safe place in priority)
            priority += 1f / (stratAI.controller.Factories[0].GetInfluencePosition() - position).SqrMagnitude();
        }
        else
        {
            // We can defend this place so it's not a priority
            priority = 0f;
        }
        
        // Apply direct coefficient depending on AI personality
        // TODO:
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
