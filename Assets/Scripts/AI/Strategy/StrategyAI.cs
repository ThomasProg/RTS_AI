using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilitySystem;

[RequireComponent(typeof(SquadManager))]
public class StrategyAI : MonoBehaviour
{
    public class Blackboard
    {
        public AIController controller;
        public SquadManager squadManager;

        public int nbUnits;
        public int nbEnemyUnits;
        public int nbBuildings;
        public int nbEnemyBuildings;

        public List<Unit> allyUnits;
        public List<Factory> allyFactories;
        public int nbBuildPoints;

        public List<Squad> idleGroups;
        public TargetBuilding[] allCapturePoints;
    }


    UtilitySystem.UtilitySystem objectif;
    UtilitySystem.UtilitySystem subjectif;

    public TaskRunner taskRunner = new TaskRunner();

    public SquadManager squadManager;

    private void Awake()
    {
        squadManager = GetComponent<SquadManager>();
    }

    public void RunCaptureStrategy(Blackboard blackboard)
    {
        taskRunner.blackboard = blackboard;
        taskRunner.AssignNewTask
        (
            new TryGetIdleGroups
            {
                falseCase = new TryBuildUnit(),
                trueCase = new SquadCapturePoint()
                //trueCase = new ActionTask 
                //{
                //     action = (TaskRunner taskRunner) => { Debug.Log("Idle units available"); taskRunner.AssignNewTask(null); }
                //}
            }
        );
    }

    // Start is called before the first frame update
    void Start()
    {
        
        


    }

    // Update is called once per frame
    void Update()
    {
        if (taskRunner.blackboard == null)
            return;

        if (taskRunner.IsRunningTask())
            taskRunner.UpdateCurrentTask();
    }
}
