using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repair : Task
{
    private Unit m_unit;
    private BaseEntity m_target;
    float LastActionDate = 0f;

    public Repair(Unit unit, BaseEntity target)
    {
        m_unit = unit;
        m_target = target;
    }

    public override void OnStart()
    {

    }

    public override void OnStop()
    {
        
    }

    public override void OnUpdate()
    {
        if (m_target == null)
        {
            OnEnd();
            return;
        }

        ComputeRepairing();
    }

    public override void OnEnd()
    {
        m_unit.ProcessNextTask();
    }

    // Repairing Task
    public bool CanRepair()
    {
        float sqrDist = (m_target.GetInfluencePosition() - m_unit.GetInfluencePosition()).sqrMagnitude;
        return sqrDist <= m_unit.UnitData.RepairDistanceMax * m_unit.GetUnitData.RepairDistanceMax && m_unit.GetUnitData.CanRepair;
    }
    
    // $$$ TODO : add repairing visual feedback
    public void ComputeRepairing()
    {
        if (CanRepair() == false)
            return;

        Transform transform = m_unit.transform;
        transform.LookAt(m_target.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > m_unit.UnitData.RepairFrequency)
        {
            LastActionDate = Time.time;

            // apply reparing
            int amount = Mathf.FloorToInt(m_unit.UnitData.RPS * m_unit.UnitData.RepairFrequency);
            m_target.Repair(amount);
        }
    }
}