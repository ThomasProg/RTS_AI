using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
        float costRatio = point / (float)aiController.Factories[0].GetBuildableFactoryData(factoryToBuildID).Cost;
        
        // Apply priority only if position to build factory is found
        if (EvaluateFactoryBuildPosition(out position, factoryToBuildID))
        {
            // More a squad is farthest from factory and more money we have, more we wan't to create factory
            priority = costRatio > 1f ? costRatio * Mathf.Sqrt(sqrtDistfarthestSquad) : 0;
        }
        else
        {
            priority = 0;
        }
    }
    
    int EvaluateFactoryToBuild()
    {
        // TODO:
        return 0;
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
        
        bool isPosFound = false;
        for (int i = 0; i < maxIteration && !isPosFound; i++)
        {
            float dst = Mathf.Pow(i / (maxIteration - 1f), pow);
            float angle = 2 * Mathf.PI * turnFaction * i;

            rst.x = buildPos.x + radius * dst * Mathf.Cos(angle);
            rst.y = buildPos.y + radius * dst * Mathf.Sin(angle);
            isPosFound = controllerFactory
                .CanPositionFactory(factoryIndex, GameUtility.ToVec3(rst));
        }

        posOut = rst;
        return isPosFound;
    }
    
    public override List<IPOITask<StrategyAI.Blackboard>> GetProcessTasks(StrategyAI.Blackboard blackboard)
    {
        List<IPOITask<StrategyAI.Blackboard>> tasks = new List<IPOITask<StrategyAI.Blackboard>>();
        constructTask.position = position;
        constructTask.idFactoryToBuild = factoryToBuildID;
        constructTask.master = stratAI.controller.Factories[0];
        tasks.Add(constructTask);
        return tasks;
    }
}
