using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilitySystem;

[RequireComponent(typeof(SquadManager))]
public class StrategyAI : MonoBehaviour
{
    public class Blackboard
    {
        public Blackboard(AIController controller, SquadManager squadManager)
        {
            this.controller = controller;
            this.squadManager = squadManager;
        }

        private AIController controller;
        private SquadManager squadManager;

        public Unit[] AllyUnits => controller.Units;
        public Factory[] AllyFactories => controller.Factories;
        public int nbBuildPoints => controller.TotalBuildPoints;

        public TargetBuilding[] allCapturePoints => GameServices.GetTargetBuildings();

        public TargetBuilding[] EnemyUnits => throw new NotImplementedException();
        public TargetBuilding[] EnemySquads => throw new NotImplementedException();
    }


    UtilitySystem.UtilitySystem objectif;
    UtilitySystem.UtilitySystem subjectif;

    public SquadManager squadManager;
    public AIController controller;

    public Blackboard bb { get; private set; } = null;
    
    private void Awake()
    {
        squadManager = GetComponent<SquadManager>();
        bb = new Blackboard(controller, squadManager);
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (TargetBuilding targetBuilding in controller.allCapturePoints)
        {
            AddTactic(new CapturePointPoI(targetBuilding) {stratAI = this, squadManager = squadManager});
        }
    }

    void AddTactic(PointOfInterest pointOfInterest)
    {
        AllPointOfInterests.Add(pointOfInterest);
    }


    public PoolTaskRunner poolTaskRunner = new PoolTaskRunner();
    public List<PointOfInterest> AllPointOfInterests = new List<PointOfInterest>();
    public List<PointOfInterest> AllPointOfInterestsByPriority = new List<PointOfInterest>();

    // Update is called once per frame
    void Update()
    {
        if (poolTaskRunner.IsRunningTask())
            poolTaskRunner.UpdateCurrentTask();
        else
        {
            foreach (var poi in AllPointOfInterests)
            {
                poi.EvaluatePriority(bb);
                // wait for seconds
            }

            // Sort by priority
            List<PointOfInterest> oldAllTacticsByPriority = AllPointOfInterestsByPriority;
            AllPointOfInterestsByPriority = new List<PointOfInterest>(AllPointOfInterests);
            AllPointOfInterestsByPriority.Sort((PointOfInterest a, PointOfInterest b) =>
                -a.priority.CompareTo(b.priority));

            int i = 0;
            while (i < AllPointOfInterestsByPriority.Count && i < oldAllTacticsByPriority.Count &&
                   AllPointOfInterestsByPriority[i] == oldAllTacticsByPriority[i])
            {
                i++;
            }
            squadManager.QueryUnit(AllPointOfInterestsByPriority[0], bb);
            List<Task> tasks = AllPointOfInterestsByPriority[0].GetProcessTasks(bb);

            foreach (var task in tasks)
            {
                poolTaskRunner.AddNewTask(task);
            }
        }
    }
}