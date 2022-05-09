using UnityEngine;

public class GoTo : Task
{
    private Unit m_unit;
    private Vector2 m_position;

    public GoTo(Unit unit, Vector2 pos)
    {
        m_unit = unit;
        m_position = pos;
    }

    public override void OnStart()
    {
        m_unit.GoTo(m_position);
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