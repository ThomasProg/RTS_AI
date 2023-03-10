using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : BaseEntity
{
    [SerializeField] public UnitDataScriptable UnitData = null;

    public Transform BulletSlot;

    private PoolTaskRunner m_taskRunner = new PoolTaskRunner();

    public Formation formation;
    public float LastActionDate = 0f;

    NavMeshAgent navMeshAgent;
    public bool IsIdle => !m_taskRunner.IsRunningTask();

    public UnitDataScriptable GetUnitData
    {
        get { return UnitData; }
    }

    public int Cost
    {
        get { return UnitData.Cost; }
    }

    public int GetTypeId
    {
        get { return UnitData.TypeId; }
    }

    public float GetStrength()
    {
        return GetUnitData.DPS;
    }

    override public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        base.Init(_team);

        HP = UnitData.MaxHP;
        OnDeadEvent += Unit_OnDead;
    }

    void Unit_OnDead(BaseEntity entity)
    {
        Stop();

        if (GetUnitData.DeathFXPrefab)
        {
            GameObject fx = Instantiate(GetUnitData.DeathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }

    public Vector2 GetDirection()
    {
        Vector3 velocity = navMeshAgent.velocity;
        return new Vector2(velocity.x, velocity.z);
    }


    public void SetSpeed(float speed)
    {
        if (navMeshAgent != null) navMeshAgent.speed = speed;
    }

    public void ResetSpeed()
    {
        if (navMeshAgent != null) navMeshAgent.speed = 0;
    }

    #region MonoBehaviour methods
    
    override protected void Awake()
    {
        base.Awake();

        navMeshAgent = GetComponent<NavMeshAgent>();
        BulletSlot = transform.Find("BulletSlot");

        // fill NavMeshAgent parameters
        navMeshAgent.speed = GetUnitData.Speed;
        navMeshAgent.angularSpeed = GetUnitData.AngularSpeed;
        navMeshAgent.acceleration = GetUnitData.Acceleration;
    }

    override protected void Start()
    {
        // Needed for non factory spawned units (debug)
        if (!IsInitialized)
            Init(Team);

        base.Start();
    }

    override protected void Update()
    {
        m_taskRunner.UpdateCurrentTask();

        // TODO: Can be checked each 3, 4 frames...
        TryProcessAutoAttack();
        TryProcessAutoRepair();
        TryProcessAutoCapture();
    }

    private void TryProcessAutoAttack()
    {
        if (IsIdle)
        {
            if (UnitData.IsAutoAttack)
            {
                Unit[] opponentUnits = GameServices.GetControllerByTeam(GameServices.GetOpponent(GetTeam())).Units;
                Factory[] opponentFactories = GameServices.GetControllerByTeam(GameServices.GetOpponent(GetTeam())).Factories;
                BaseEntity target = null;
                float targetDistance = float.MaxValue;

                foreach (Unit unit in opponentUnits)
                {
                    float sqrtDist = (unit.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude;
                    if (sqrtDist < UnitData.AttackDistanceMax * UnitData.AttackDistanceMax &&
                        (target == null || sqrtDist < targetDistance))
                    {
                        targetDistance = sqrtDist;
                        target = unit;
                    }
                }
                
                // Attack factory only if unity can't be attacked
                if (target == null)
                {
                    foreach (Factory factory in opponentFactories)
                    {
                        float sqrtDist = (factory.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude - factory.Size * factory.Size;
                        if (sqrtDist < UnitData.AttackDistanceMax * UnitData.AttackDistanceMax &&
                            (target == null || sqrtDist < targetDistance))
                        {
                            targetDistance = sqrtDist;
                            target = factory;
                        }
                    }
                }

                if (target != null)
                    SetTaskAttackTarget(target);
            }
        }
    }

    private void TryProcessAutoRepair()
    {
        // Check if is always IsIdle
        if (IsIdle)
        {
            if (UnitData.IsAutoRepair && UnitData.CanRepair)
            {
                Unit[] units = GameServices.GetControllerByTeam(GetTeam()).Units;
                Unit target = null;
                float targetDistance = float.MaxValue;

                foreach (Unit unit in units)
                {
                    float sqrtDist = (unit.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude;
                    if (unit.NeedsRepairing() && sqrtDist < UnitData.RepairDistanceMax * UnitData.RepairDistanceMax &&
                        (target == null || sqrtDist < targetDistance))
                    {
                        targetDistance = sqrtDist;
                        target = unit;
                    }
                }

                if (target != null)
                    SetTaskRepairTarget(target);
            }
        }
    }

    private void TryProcessAutoCapture()
    {
        // Check if is always IsIdle
        if (IsIdle)
        {
            if (UnitData.IsAutoCapture && UnitData.IsAutoCapture)
            {
                TargetBuilding[] buildings = GameServices.GetTargetBuildings();
                TargetBuilding target = null;
                float targetDistance = float.MaxValue;

                foreach (TargetBuilding building in buildings)
                {
                    float sqrtDist = (building.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude;
                    if (building.GetTeam() != GetTeam() &&
                        sqrtDist < UnitData.CaptureDistanceMax * UnitData.CaptureDistanceMax &&
                        (target == null || sqrtDist < targetDistance))
                    {
                        targetDistance = sqrtDist;
                        target = building;
                    }
                }

                if (target != null)
                    SetTaskCaptureTarget(target);
            }
        }
    }

    #endregion

    #region IRepairable

    override public bool NeedsRepairing()
    {
        return HP < GetUnitData.MaxHP;
    }


    override public void Repair(int amount)
    {
        HP = Mathf.Min(HP + amount, GetUnitData.MaxHP);
        base.Repair(amount);
    }


    override public void FullRepair()
    {
        Repair(GetUnitData.MaxHP);
    }


    #endregion

    #region Tasks methods : Moving, Capturing, Targeting, Attacking, Repairing ...

    public void ProcessNextTask()
    {
        m_taskRunner.ProcessNextTask();
    }

    public void GoTo(Vector2 pos, float stoppingDistance = 1f)
    {
        if (!navMeshAgent.isOnNavMesh)
            return;

        navMeshAgent.stoppingDistance = stoppingDistance;
        
        // See : https://youtu.be/bqtqltqcQhw?t=329
        bool isdestinationFound = false;
        float turnFaction = 0.618033f;
        float pow  = 0.5f;
        float radius = 20;
        
        int maxIteration = 10;
        for (int i = 0; i < maxIteration && !isdestinationFound; i++)
        {
            float dst = Mathf.Pow(i / (maxIteration - 1f), pow);
            float angle = 2 * Mathf.PI * turnFaction * i;

            float x = pos.x + radius * dst * Mathf.Cos(angle);
            float y = pos.y + radius * dst * Mathf.Sin(angle);
            isdestinationFound = navMeshAgent.SetDestination(new Vector3(x, 0f, y));
        }
        
        navMeshAgent.isStopped = false;
    }

    public bool IsDestinationReached()
    {
        return navMeshAgent.isOnNavMesh && !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance &&
               (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f);
    }

    public void StopMovement()
    {
        navMeshAgent.isStopped = true;
    }

    // $$$ To be updated for AI implementation $$$

    // Moving Task
    public void AddTaskGoTo(Vector3 pos, float stoppingDistance = 1f)
    {
        m_taskRunner.AddNewTask(new GoToAndDefend(this, new Vector2(pos.x, pos.z), stoppingDistance));
    }

    public void AddTask(Task newTask)
    {
        m_taskRunner.AddNewTask(newTask);
    }

    public Task[] GetRemainingTasks()
    {
        return m_taskRunner.tasks.ToArray();
    }

    public void AddTaskGoToAndDefend(Vector3 pos, float stoppingDistance = 1f)
    {
        m_taskRunner.AddNewTask(new GoToAndDefend(this, new Vector2(pos.x, pos.z), stoppingDistance));
    }

    // Targetting Task - attack
    public void AddTaskAttackTarget(BaseEntity target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AddNewTask(new Attack(this, target));
        }
    }

    // Targetting Task - capture
    public void AddTaskCaptureTarget(TargetBuilding target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AddNewTask(new CaptureTarget(this, target));
        }
    }

    // Targetting Task - repairing
    public void AddTaskRepairTarget(BaseEntity target)
    {
        if (target.GetTeam() == GetTeam())
        {
            m_taskRunner.AddNewTask(new Repair(this, target));
        }
    }
    
    
    // Moving Task
    public void SetTaskGoTo(Vector3 pos, float stoppingDistance = 1f)
    {
        m_taskRunner.AssignNewTask(new GoTo(this, new Vector2(pos.x, pos.z), stoppingDistance));
    }

    // Targetting Task - attack
    public void SetTaskAttackTarget(BaseEntity target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AssignNewTask(new Attack(this, target));
        }
    }

    // Targetting Task - capture
    public void SetTaskCaptureTarget(TargetBuilding target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AssignNewTask(new CaptureTarget(this, target));
        }
    }

    // Targetting Task - repairing
    public void SetTaskRepairTarget(BaseEntity target)
    {
        if (target.GetTeam() == GetTeam())
        {
            m_taskRunner.AssignNewTask(new Repair(this, target));
        }
    }

    public void Stop()
    {
        StopMovement();
        m_taskRunner.StopCurrentTask();
        m_taskRunner.Clear();
    }
    
    public override float GetVisibilityRadius()
    {
        return Mathf.Max(UnitData.AttackDistanceMax, UnitData.CaptureDistanceMax, UnitData.RepairDistanceMax) * 1.1f;
    }

    #endregion
}