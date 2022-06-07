using UnityEngine;

public class GoToAndDefend : Task
{
    private Unit m_unit;
    private Vector2 m_position;
    private float m_stoppingDistance;

    public GoToAndDefend(Unit unit, Vector2 pos, float stoppingDistance = 1f)
    {
        m_unit = unit;
        m_position = pos;
        m_stoppingDistance = stoppingDistance;
    }

    public override void OnStart()
    {
        m_unit.OnTakeDamage += QueryHelpAndAttackTarget;
        
        m_unit.GoTo(m_position, m_stoppingDistance);
    }

    void QueryHelpAndAttackTarget(BaseEntity damageProvider)
    {
        if (damageProvider == null)
            return;
        
        m_unit.OnTakeDamage -= QueryHelpAndAttackTarget;
        Task[] tasks = m_unit.GetRemainingTasks();
        m_unit.Stop();
        m_unit.AddTaskAttackTarget(damageProvider);

        foreach (Task task in tasks)
        {
            m_unit.AddTask(task);
        }
    }

    public override void OnStop()
    {
        m_unit.StopMovement();
    }

    public override void OnUpdate()
    {
        if (m_unit.IsDestinationReached())
            OnEnd();
    }

    public override void OnEnd()
    {
        m_unit.ProcessNextTask();
    }
}