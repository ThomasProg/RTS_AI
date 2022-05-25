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

        foreach (Factory factory in controller.Factories)
        {
            AddTactic(new FactoryPoI(factory) {stratAI = this, squadManager = squadManager});
        }

        foreach (Factory factory in GameServices.GetPlayerController().Factories)
        {
            AddTactic(new FactoryPoI(factory) {stratAI = this, squadManager = squadManager});
        }
        
        AddTactic(new ConstructFactoryPoI() {stratAI = this, squadManager = squadManager});
        
        StartCoroutine(UpdateInterests());
        
        GameServices.GetPlayerController().OnCreateFactory += factory =>  AddTactic(new FactoryPoI(factory) {stratAI = this, squadManager = squadManager}); 
        controller.OnCreateFactory += factory =>  AddTactic(new FactoryPoI(factory) {stratAI = this, squadManager = squadManager}); 
    }

    void AddTactic(PointOfInterest pointOfInterest)
    {
        AllPointOfInterests.Add(pointOfInterest);
    }


    //public PriorityTaskRunner priorityTaskRunner = new PriorityTaskRunner();
    public List<PointOfInterest> AllPointOfInterests = new List<PointOfInterest>();
    public List<PointOfInterest> AllPointOfInterestsByPriority = new List<PointOfInterest>();

    // Return the nb of seconds to wait
    WaitForSeconds RunTasks(List<List<IEnumerator>> tasksEnumerators)
    {
        for (int i = 0; i < tasksEnumerators.Count; i++)
        {
            for (int j = 0; j < tasksEnumerators[i].Count; j++)
            {
                IEnumerator enumerator = tasksEnumerators[i][j];

                bool isFinished = !enumerator.MoveNext();
                object obj = enumerator.Current;

                if (obj is WaitForSeconds waitForSeconds)
                    return waitForSeconds;

                if (!isFinished)
                    break;
            }
        }

        return null;
    }



    // Update is called once per frame
    IEnumerator UpdateInterests()
    {
        yield return null;

        while (true)
        {
            // Update player squads
            {
                ETeam playerTeam = GameServices.GetPlayerController().Team;
                //Remove previous player squad
                AllPointOfInterests.RemoveAll(interest =>
                    interest is SquadPoI squadPoI && squadPoI.squad.GetTeam() == playerTeam);

                // Add squad to PoI list
                foreach (Squad squad in controller.PlayerSquads)
                {
                    AddTactic(new SquadPoI(squad) {stratAI = this, squadManager = squadManager});
                }
            }

            foreach (var poi in AllPointOfInterests)
            {
                poi.EvaluatePriority(bb);
                // wait for seconds
            }

            // Sort by priority
            AllPointOfInterestsByPriority = new List<PointOfInterest>(AllPointOfInterests);
            AllPointOfInterestsByPriority.Sort((PointOfInterest a, PointOfInterest b) =>
                -a.priority.CompareTo(b.priority));

            List<List<IPOITask<StrategyAI.Blackboard>>> tasks = new List<List<IPOITask<StrategyAI.Blackboard>>>(AllPointOfInterestsByPriority.Count);
            List<List<IEnumerator>> tasksEnumerators = new List<List<IEnumerator>>(AllPointOfInterestsByPriority.Count);
            for (int i = 0; i < AllPointOfInterestsByPriority.Count; i++)
            {
                tasks.Add(AllPointOfInterestsByPriority[i].GetProcessTasks(bb));
                tasksEnumerators.Add(new List<IEnumerator>(tasks[i].Count));
                for (int j = 0; j < tasks[i].Count; j++)
                {
                    tasksEnumerators[i].Add(tasks[i][j].Execute(bb));
                }
            }

            // TODO : loop until a certain amount of time without reevaluatioon priorities
            {
                WaitForSeconds waitForSeconds = RunTasks(tasksEnumerators);

                yield return waitForSeconds;
            }
        }


    }


}