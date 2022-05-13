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

    StrategyAI strategyAI;

    #region MonoBehaviour methods

    public override Squad[] Squads
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
            factory.OnUnitBuiltPersistent += (Unit unit) =>
            {
                strategyAI.squadManager.RegisterSquad(new Squad(unit));
            };
        }
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
