using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// $$$ TO DO :)
[RequireComponent(typeof(StrategyAI))]
public sealed class AIController : UnitController
{
    public TargetBuilding[] allCapturePoints;

    public StrategyAI strategyAI;
    private Squad[] playerSquadsEvaluated;
    private int currentFrame = 0;



    #region MonoBehaviour methods

    public Squad[] PlayerSquads
    {
        get
        {
            if (Time.frameCount != currentFrame)
            {
                currentFrame = Time.frameCount;
                playerSquadsEvaluated = Squad.MakeSquadsDependingOnDistance(GameServices.GetPlayerController().Units, 10).ToArray();
            }
            return playerSquadsEvaluated;
        }
    }
    
    public Squad[] Squads
    {
        get
        {
            return strategyAI.squadManager.squads.ToArray();
        }
    }

    protected override void Awake()
    {
        base.Awake();

        strategyAI = GetComponent<StrategyAI>();
        strategyAI.controller = this;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        allCapturePoints = FindObjectsOfType(typeof(TargetBuilding)) as TargetBuilding[];

        foreach (Factory factory in Factories)
        {
            strategyAI.squadManager.LinkToAI(factory);
        }
    }

    public bool RequestFactoryBuild(Factory master, int factoryIndex, Vector3 buildPos)
    {
        if (master == null)
            return false;

        int cost = master.GetFactoryCost(factoryIndex);
        if (TotalBuildPoints < cost)
            return false;

        // Check if positon is valid
        if (master.CanPositionFactory(factoryIndex, buildPos) == false)
            return false;

        Factory newFactory = master.StartBuildFactory(factoryIndex, buildPos);
        if (newFactory != null)
        {
            AddFactory(newFactory);
            TotalBuildPoints -= cost;
            strategyAI.squadManager.LinkToAI(newFactory);

            return true;
        }
        return false;
    }
    
    protected override void Update()
    {
        base.Update();

        //strategyAI.blackboard = new StrategyAI.Blackboard
        //{
        //    controller = this,
        //    allyUnits = UnitList,
        //    allyFactories = FactoryList,
        //    nbBuildPoints = TotalBuildPoints,
        //    allCapturePoints = allCapturePoints,
        //    squadManager = strategyAI.squadManager
        //};

        //if (!strategyAI.taskRunner.IsRunningTask())
        //{
        //    //StrategyAI.Blackboard blackboard = new StrategyAI.Blackboard
        //    //{
        //    //    controller = this,
        //    //    allyUnits = UnitList,
        //    //    allyFactories = FactoryList,
        //    //    nbBuildPoints = TotalBuildPoints,
        //    //    allCapturePoints = allCapturePoints,
        //    //    squadManager = strategyAI.squadManager
        //    //};
        //    //strategyAI.RunCaptureStrategy(blackboard);
        //}
    }

    #endregion
}
