using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class ConstructFactoryPoI : PointOfInterest
{
    public StrategyAI stratAI;
    Squad farthestSquad = null;
    private int factoryToBuildID;
    private ConstructFactoryTask constructTask;
    
    public ConstructFactoryPoI()
    {
        constructTask = new ConstructFactoryTask();
    }

    public override void EvaluatePriority(StrategyAI.Blackboard blackboard)
    {
        AIController aiController = stratAI.controller;

        if (aiController.Squads.Length == 0)
        {
            priority = 0;
            return;
        }

        // Get the distance from squad to nearest factory
        float sqrtDistfarthestSquad = float.MinValue;
        foreach (Squad squad in aiController.Squads)
        {
            float sqrtDistSquadToNearestFactory = float.MaxValue;
            foreach (Factory factory in aiController.Factories)
            {
                float sqrtDistSquadToFactory = (squad.GetAveragePosition() - factory.GetInfluencePosition()).sqrMagnitude;
                sqrtDistSquadToNearestFactory = Mathf.Min(sqrtDistSquadToNearestFactory, sqrtDistSquadToFactory);
            }

            if (sqrtDistSquadToNearestFactory > sqrtDistfarthestSquad)
            {
                sqrtDistfarthestSquad = Mathf.Max(sqrtDistfarthestSquad, sqrtDistSquadToNearestFactory);
                farthestSquad = squad;
            }
        }
        
        // Evaluate the cost ratio
        int point = aiController.TotalBuildPoints;
        factoryToBuildID = EvaluateFactoryToBuild();
        priority = 0;
        
        if (factoryToBuildID == -1)
            return;
        

        float costRatio = point / (float)aiController.Factories[0].GetBuildableFactoryData(factoryToBuildID).Cost;
        
        // Apply priority only if position to build factory is found
        float costRatioAcceptance = 1 + stratAI.subjectiveUtilitySystem.GetStat("Patience").Value;
        if (EvaluateFactoryBuildPosition(out position, factoryToBuildID) && costRatio > costRatioAcceptance)
        {
            // More a squad is farthest from factory and more money we have, more we wan't to create factory
            priority = costRatio * Mathf.Sqrt(sqrtDistfarthestSquad);
        }
    }
    
    int EvaluateFactoryToBuild()
    {
        // Get all buildable factory
        FactoryDataScriptable tiere1Factory = null;
        FactoryDataScriptable tiere2Factory = null;
        int idTiere1Factory = 0;
        int idTiere2Factory = 0;
        
        for (int i = 0; i < stratAI.controller.Factories[0].FactoryPrefabsCount; i++)
        {
            FactoryDataScriptable factory = stratAI.controller.Factories[0].GetBuildableFactoryData(i);
            switch (factory.Caption)
            {
                case "Light Factory":
                    tiere1Factory = factory;
                    idTiere1Factory = i;
                    break;
                
                case "Heavy Factory":
                    tiere2Factory = factory;
                    idTiere2Factory = i;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        float IAAnticipation = stratAI.subjectiveUtilitySystem.GetStat("Anticipation").Value;
        float costAnticipationRatio = (1f + IAAnticipation / 2f);

        if (stratAI.controller.TotalBuildPoints < tiere1Factory.Cost * costAnticipationRatio)
        {
            return -1; // No enough money to construct factory
        }

        // Evaluate chance to construct tiere 2 factory
        float constructFactoryTiere2Chance = 0f;

        float rationPOIOccupiedByIA = 0f;
                
        rationPOIOccupiedByIA += GameServices.GetAIController().CapturedTargets /
            (float) GameServices.GetTargetBuildings().Length;

        constructFactoryTiere2Chance = rationPOIOccupiedByIA;

        if (GameServices.GetPlayerController().IsTiere2())
        {
            constructFactoryTiere2Chance += IAAnticipation;
        }

        float randomUnitValue = UnityEngine.Random.value;

        if (randomUnitValue > 1f - Mathf.Min(constructFactoryTiere2Chance, 1f))
        {
            if (stratAI.controller.TotalBuildPoints > tiere2Factory.Cost * costAnticipationRatio)
                return idTiere2Factory;
            else
                return -1;
        }
        else
        {
            return idTiere1Factory;
        }
    }

    /// <summary>
    /// Evaluate best position to place factory
    /// </summary>
    /// <param name="posOut"></param>
    /// <param name="factoryIndex"></param>
    /// <param name="buildPos"></param>
    /// <returns>true if position find, else false</returns>
    bool EvaluateFactoryBuildPosition(out Vector2 posOut, int factoryIndex)
    {
        int maxIteration = 10;
        float turnFaction = 0.618033f;
        float pow  = 0.5f;
        float radius = 10;

        Vector2 rst = new Vector2();
        Factory controllerFactory = stratAI.controller.Factories[0];
        FactoryDataScriptable factoryData = controllerFactory.GetBuildableFactoryData(factoryIndex);
        Vector2 buildPos = farthestSquad.GetInfluencePosition() - (Mathf.Sqrt(farthestSquad.GetInfluenceRadius()) + factoryData.SpawnRadius) * Vector2.down;
        
        // Iterate on a circle to find best position
        bool isPosFound = false;
        for (int i = 0; i < maxIteration && !isPosFound; i++)
        {
            float dst = Mathf.Pow(i / (maxIteration - 1f), pow);
            float angle = 2 * Mathf.PI * turnFaction * i;

            rst.x = buildPos.x + radius * dst * Mathf.Cos(angle);
            rst.y = buildPos.y + radius * dst * Mathf.Sin(angle);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(GameUtility.ToVec3(rst), out hit, dst, 1))
            {
                rst.x = hit.position.x;
                rst.y = hit.position.z;
                isPosFound = controllerFactory.CanPositionFactory(factoryIndex, GameUtility.ToVec3(rst));
            }
        }

        posOut = rst;
        return isPosFound;
    }
    
    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        if (priority < Mathf.Epsilon)
        {
            Debug.LogWarning("Constructing a factory with priority 0");
        }
        else
        {
            Debug.Log($"Constructing a factory with priority {priority}");

        }
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        constructTask.position = position;
        constructTask.idFactoryToBuild = factoryToBuildID;
        constructTask.master = stratAI.controller.Factories[0];
        tasks.Add(constructTask);
        return tasks;
    }
}
