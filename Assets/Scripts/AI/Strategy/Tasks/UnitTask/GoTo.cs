using UnityEngine;

public class GoTo : Task
{
    private Unit m_unit;
    private Vector2 m_position;
    private float m_stoppingDistance;

    public GoTo(Unit unit, Vector2 pos, float stoppingDistance = 1f)
    {
        m_unit = unit;
        m_position = pos;
        m_stoppingDistance = stoppingDistance;
    }

    public override void OnStart()
    {
        m_unit.GoTo(m_position, m_stoppingDistance);
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