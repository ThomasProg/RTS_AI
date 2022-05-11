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

    public SquadManager squadManager;

    public AIController controller;

    private void Awake()
    {
        squadManager = GetComponent<SquadManager>();

        TargetBuilding[] allBuildings = FindObjectsOfType(typeof(TargetBuilding)) as TargetBuilding[];

        foreach (TargetBuilding targetBuilding in  allBuildings)
        {
            //squadTasks.Add(new );
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        AddTactic(new MakeUnitsTactic { stratAI = this });

        foreach (TargetBuilding targetBuilding in controller.allCapturePoints)
        {
            AddTactic(new CapturePointTactic(targetBuilding) { stratAI = this, squadManager = squadManager } );
        }


    }

    void AddTactic(Tactic tactic)
    {
        alltacticsByEvaluationPriority.Add(tactic);
    }


    public TaskRunner taskRunner = new TaskRunner();
    public List<Tactic> alltacticsByEvaluationPriority = new List<Tactic>();
    public List<Tactic> allTacticsByPriority = new List<Tactic>();

    // Update is called once per frame
    void Update()
    {
        if (taskRunner.IsRunningTask())
            taskRunner.UpdateCurrentTask();
        else
        {
            alltacticsByEvaluationPriority.Sort((Tactic a, Tactic b) => a.GetEvaluationFrequencyScore().CompareTo(b.GetEvaluationFrequencyScore()));

            for (int k = 0; k < alltacticsByEvaluationPriority.Count && alltacticsByEvaluationPriority[k].GetEvaluationFrequencyScore() < 0f; k++)
            {
                alltacticsByEvaluationPriority[k].EvaluatePriority();
                alltacticsByEvaluationPriority[k].lastEvaluationTime = Time.time;
                // wait for seconds
            }

            // Sort by priority
            List<Tactic> oldAllTacticsByPriority = allTacticsByPriority;
            allTacticsByPriority = new List<Tactic>(alltacticsByEvaluationPriority);
            allTacticsByPriority.Sort((Tactic a, Tactic b) => -a.priority.CompareTo(b.priority));

            int i = 0;
            while (i < allTacticsByPriority.Count && i < oldAllTacticsByPriority.Count && allTacticsByPriority[i] == oldAllTacticsByPriority[i])
            {
                i++;
            }

            //// If not equal to before
            //if (!(i == allTacticsByPriority.Count && i == oldAllTacticsByPriority.Count))
            //{
            //    taskRunner.StopCurrentTask();

                InBetweenTask firstTask = allTacticsByPriority[0].GetProcessTask();
                InBetweenTask lastTask = firstTask;
                // Construct new plan
                for (int j = 1; j < allTacticsByPriority.Count; j++)
                {
                    InBetweenTask next = allTacticsByPriority[j].GetProcessTask();
                    lastTask.next = next;
                    lastTask = next;
                }
                lastTask.next = null;

                // Assign new plan
                taskRunner.AssignNewTask(firstTask);
            //}

            // TODO : try to update the graph without changing the taskrunner's current node


            //bool currentTacticFound = false;

            //// if the tactics has changed order, stops current task, and process tasks from last change
            //int i = 0;
            //while (i < allTacticsByPriority.Count && i < oldAllTacticsByPriority.Count && allTacticsByPriority[i] == oldAllTacticsByPriority[i])
            //{
            //    if (taskRunner.IsTaskRunning(allTacticsByPriority[i].GetProcessTask()))
            //        currentTacticFound = true;
            //    i++;
            //}

            //if  (currentTacticFound)
            //{

            //}
            //if (i < allTacticsByPriority.Count && i < oldAllTacticsByPriority.Count && allTacticsByPriority[i] != oldAllTacticsByPriority[i])
            //{

            //}

            //// if has changed
            //if (i < allTacticsByPriority.Count)
            //{
            //    // Stop previous task
            //    taskRunner.StopCurrentTask();

            //    Task firstTask = allTacticsByPriority[i].GetProcessTask();
            //    i++;
            //    Task lastTask = firstTask;
            //    // Construct new plan
            //    while (i < allTacticsByPriority.Count)
            //    {
            //        lastTask.next = allTacticsByPriority[i].GetProcessTask();
            //    }
            //    // Assign new plan
            //    taskRunner.AssignNewTask(firstTask);
            //}
        }
    }
}
