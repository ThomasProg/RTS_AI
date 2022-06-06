using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilitySystemPackage;

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

    [SerializeField] private UtilitySystemData objectiveData;
    [SerializeField] private UtilitySystemData subjectiveData;

    public UtilitySystem objectiveUtilitySystem = new UtilitySystem();
    public UtilitySystem subjectiveUtilitySystem = new UtilitySystem();

    public SquadManager squadManager;
    public AIController controller;

    float priorityEvaluationDelay = 5;

    public Blackboard bb { get; private set; } = null;

    private void Awake()
    {
        squadManager = GetComponent<SquadManager>();
        bb = new Blackboard(controller, squadManager);

        objectiveUtilitySystem.Init(objectiveData);
        subjectiveUtilitySystem.Init(subjectiveData);

        //priorityTaskRunner.blackboard = bb;
    }

    void CreateFactoryPoI(Factory factory)
    {
        FactoryPoI factoryPoI = new FactoryPoI(factory) {stratAI = this, squadManager = squadManager};
        AddTactic(factoryPoI);
        factory.OnDeadEvent += current => AllPointOfInterests.Remove(factoryPoI);
    }

    void CreateSquadPoI(Squad squad)
    {
        SquadPoI newSquadPoI = new SquadPoI(squad) {stratAI = this, squadManager = squadManager};
        AddTactic(newSquadPoI);
        squad.OnSquadEmpty += current => AllPointOfInterests.Remove(newSquadPoI);
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
            CreateFactoryPoI(factory);
        }

        foreach (Factory factory in GameServices.GetPlayerController().Factories)
        {
            CreateFactoryPoI(factory);
        }

        AddTactic(new ConstructFactoryPoI() {stratAI = this, squadManager = squadManager});

        StartCoroutine(UpdateInterests());

        GameServices.GetPlayerController().OnCreateFactory += CreateFactoryPoI;
        controller.OnCreateFactory += CreateFactoryPoI;
        ;
    }

    void AddTactic(PointOfInterest pointOfInterest)
    {
        AllPointOfInterests.Add(pointOfInterest);
    }


    //public PriorityTaskRunner priorityTaskRunner = new PriorityTaskRunner();
    public List<PointOfInterest> AllPointOfInterests = new List<PointOfInterest>();
    public List<PointOfInterest> AllPointOfInterestsByPriority = new List<PointOfInterest>();

    // Return the nb of seconds to wait
    IEnumerator RunTasks(List<List<IEnumerator>> tasksEnumerators)
    {
        while (true)
        {
            objectiveUtilitySystem.Update();
            var utilities = objectiveUtilitySystem.GetUtilities();

            var dict = new Dictionary<string, float>();
            foreach (var utility in utilities.Values)
            {
                dict[utility.Name] = utility.Value;
            }

            subjectiveUtilitySystem.UpdateStats(dict, true);
            subjectiveUtilitySystem.Update();

            for (int i = 0; i < tasksEnumerators.Count; i++)
            {
                for (int j = 0; j < tasksEnumerators[i].Count; j++)
                {
                    IEnumerator enumerator = tasksEnumerators[i][j];

                    bool isFinished = !enumerator.MoveNext();
                    object obj = enumerator.Current;

                    if (!isFinished)
                    {
                        //if (obj is WaitForSeconds waitForSeconds)
                        //{
                        //    yield return obj;
                        //    //yield break;
                        //}

                        yield return null;
                        break;
                    }
                }

                yield return null;
            }

            yield return null;
        }
    }


    // Update is called once per frame
    IEnumerator UpdateInterests()
    {
        yield return null; // Keep to run after start

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
                    CreateSquadPoI(squad);
                }
            }

            foreach (var poi in AllPointOfInterests)
            {
                poi.EvaluatePriority(bb);
                yield return null;
                // wait for seconds
            }

            // Sort by priority
            AllPointOfInterestsByPriority = new List<PointOfInterest>(AllPointOfInterests);
            AllPointOfInterestsByPriority.Sort((PointOfInterest a, PointOfInterest b) =>
                -a.priority.CompareTo(b.priority));

            List<List<IPOITask<StrategyAI.Blackboard>>> tasks =
                new List<List<IPOITask<StrategyAI.Blackboard>>>(AllPointOfInterestsByPriority.Count);
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

            float lastPriorityUpdate = Time.time;

            IEnumerator enumerator = RunTasks(tasksEnumerators);

            // TODO : loop until a certain amount of time without reevaluatioon priorities
            while (lastPriorityUpdate + priorityEvaluationDelay > Time.time)
            {
                //yield return RunTasks(tasksEnumerators);
                if (!enumerator.MoveNext())
                    break;

                //yield return new WaitForSeconds(0.1f);
                yield return enumerator.Current;
            }
        }
    }
}