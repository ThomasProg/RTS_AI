using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilitySystem;

public class StrategyAI : MonoBehaviour
{
    public class Blackboard
    {
        public AIController controller;

        public int nbUnits;
        public int nbEnemyUnits;
        public int nbBuildings;
        public int nbEnemyBuildings;

        //public float timer = 0f;

        public List<Unit> allyUnits;
        public List<Factory> allyFactories;
        public int nbBuildPoints;
    }


    UtilitySystem.UtilitySystem objectif;
    UtilitySystem.UtilitySystem subjectif;

    public TaskRunner taskRunner = new TaskRunner();

    private void Awake()
    {
        //taskRunner = new TaskRunner();
        //taskRunner.blackboard = new Blackboard();

        //taskRunner.AssignNewTask
        //(
        //    new HasEnoughBuildings
        //    {
        //        falseCase = new TryCreateBuildingsTask(),
        //        trueCase = new HasEnoughUnits
        //        {
        //            falseCase = new TryCreateUnitsTask(),
        //            trueCase = new TryAttackTask()
        //        },
        //    }
        //);
    }

    public void RunCaptureStrategy(Blackboard blackboard)
    {
        taskRunner.blackboard = blackboard;
        taskRunner.AssignNewTask
        (
            new TryGetIdleGroups
            {
                falseCase = new TryBuildUnit(),
                trueCase = new ActionTask 
                {
                     action = (TaskRunner taskRunner) => { Debug.Log("Idle units available"); taskRunner.AssignNewTask(null); }
                }
                //trueCase = new HasEnoughUnits
                //{
                //    falseCase = new TryCreateUnitsTask(),
                //    trueCase = new TryAttackTask()
                //},
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

        //Blackboard bb = (Blackboard) taskRunner.blackboard;
        //if (bb.timer < 0f)
        if (taskRunner.IsRunningTask())
            taskRunner.UpdateCurrentTask();
        //else
            //bb.timer -= Time.deltaTime;
    }
}
