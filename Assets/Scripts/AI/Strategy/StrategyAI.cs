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
        public SquadManager squadManager;

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
        //priorityTaskRunner.blackboard = bb;
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        foreach (TargetBuilding targetBuilding in controller.allCapturePoints)
        {
            AddTactic(new CapturePointPoI(targetBuilding) {stratAI = this, squadManager = squadManager});
        }
        
        foreach (Factory factory in GameServices.GetPlayerController().Factories)
        {
            AddTactic(new FactoryPoI(factory) {stratAI = this, squadManager = squadManager});
        }
        
        foreach (Factory factory in controller.Factories)
        {
            AddTactic(new FactoryPoI(factory) {stratAI = this, squadManager = squadManager});
        }

        StartCoroutine(UpdateInterests());
    }

    void AddTactic(PointOfInterest pointOfInterest)
    {
        AllPointOfInterests.Add(pointOfInterest);
    }


    //public PriorityTaskRunner priorityTaskRunner = new PriorityTaskRunner();
    public List<PointOfInterest> AllPointOfInterests = new List<PointOfInterest>();
    public List<PointOfInterest> AllPointOfInterestsByPriority = new List<PointOfInterest>();

    // Update is called once per frame
    IEnumerator UpdateInterests()
    {
        yield return null;

        while (true)
        {
            foreach (var poi in AllPointOfInterests)
            {
                poi.EvaluatePriority(bb);
                // wait for seconds
            }

            // Sort by priority
            AllPointOfInterestsByPriority = new List<PointOfInterest>(AllPointOfInterests);
            AllPointOfInterestsByPriority.Sort((PointOfInterest a, PointOfInterest b) =>
                -a.priority.CompareTo(b.priority));

            foreach (PointOfInterest poi in AllPointOfInterestsByPriority)
            {
                List<IPOITask<StrategyAI.Blackboard>> tasks = poi.GetProcessTasks(bb);

                foreach (var task in tasks)
                {
                    bool isFinished;
                    IEnumerator enumerator = task.Execute(bb);

                    do
                    {
                        isFinished = !enumerator.MoveNext();
                    } while (!isFinished && !(enumerator.Current is Wip));


                    if (!isFinished)
                        break;
                }
            }

            yield return null;
        }


    }


}