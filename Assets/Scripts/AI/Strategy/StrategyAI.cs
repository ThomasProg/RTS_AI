using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilitySystem;

public class StrategyAI : MonoBehaviour
{
    public class Blackboard
    {
        public int nbUnits;
        public int nbEnemyUnits;
        public int nbBuildings;
        public int nbEnemyBuildings;
    }


    UtilitySystem.UtilitySystem objectif;
    UtilitySystem.UtilitySystem subjectif;

    TaskRunner taskRunner = new TaskRunner();

    private void Awake()
    {
        taskRunner.blackboard = new Blackboard();

        taskRunner.AssignNewTask
        (
            new HasEnoughBuildings
            {
                falseCase = new TryCreateBuildingsTask(),
                trueCase = new HasEnoughUnits
                {
                    falseCase = new TryCreateUnitsTask(),
                    trueCase = new TryAttackTask()
                },
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
        taskRunner.UpdateCurrentTask();
    }
}
