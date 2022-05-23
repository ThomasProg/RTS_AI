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
    protected List<Squad> playerSquad = new List<Squad>();

    private Squad[] PlayerSquad => playerSquad.ToArray();

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
                
                // Add new player squad 
                playerSquad = Squad.MakeSquadsDependingOnDistance(GameServices.GetPlayerController().Units, 50f);
                
                // Add squad to PoI list
                foreach (Squad squad in playerSquad)
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
            List<PointOfInterest> oldAllTacticsByPriority = AllPointOfInterestsByPriority;
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
                        yield return null;
                    } while (!isFinished && !(enumerator.Current is Wip));


                    yield return null;
                    if (!isFinished)
                        break;
                }
                yield return null;
            }

            yield return null;
        }


    }


}