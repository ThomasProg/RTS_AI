using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : Task
{
    private Unit m_unit;
    private BaseEntity m_target;

    public Attack(Unit unit, BaseEntity target)
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

        ComputeAttack();
    }

    public override void OnEnd()
    {
        m_unit.ProcessNextTask();
    }
    
    public bool CanAttack()
    {
        float sqrDist = (m_target.GetInfluencePosition() - m_unit.GetInfluencePosition()).sqrMagnitude;
        return sqrDist <= m_unit.UnitData.AttackDistanceMax * m_unit.GetUnitData.AttackDistanceMax * 9 // if unit is further than 3 times its range, give up the attack
               && m_target.GetTeam() != m_unit.GetTeam();
    }

    private bool IsCloseEnough()
    {
        return (m_target.GetInfluencePosition() - m_unit.GetInfluencePosition()).sqrMagnitude <
            m_unit.UnitData.AttackDistanceMax * m_unit.GetUnitData.AttackDistanceMax;
    }
    
    public void ComputeAttack()
    {
        if (CanAttack() == false || m_target == null)
        {
            OnEnd();
            return;
        }
        if (!IsCloseEnough())
        {
            m_unit.GoTo(m_target.GetInfluencePosition(), m_unit.GetUnitData.AttackDistanceMax);
        }

        Transform transform = m_unit.transform;
        transform.LookAt(m_target.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - m_unit.LastActionDate) > m_unit.UnitData.AttackFrequency)
        {
            m_unit.LastActionDate = Time.time;
            // visual only ?
            if (m_unit.UnitData.BulletPrefab)
            {
                GameObject newBullet = Object.Instantiate(m_unit.UnitData.BulletPrefab, m_unit.BulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(m_target.transform.position - m_unit.transform.position, m_unit);
            }
            // apply damages
            int damages = Mathf.FloorToInt(m_unit.UnitData.DPS * m_unit.UnitData.AttackFrequency);
            m_target.AddDamage(m_unit, damages);
        }
    }
}