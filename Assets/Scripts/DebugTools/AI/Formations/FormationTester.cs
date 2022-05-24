using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI.Formations
{
    [ExecuteInEditMode]
    public class FormationTester : MonoBehaviour
    {
        public enum FormationType
        {
            Square,
            Circle,
            Wing,
        }

        private Squad _squad;
        [SerializeField] private Formation _formation;
        [SerializeField] private List<Unit> _units;

        public bool TPUnits = false;
        public FormationType Type;

        private void OnValidate()
        {
            _units = FindObjectsOfType<Unit>().ToList();
            _squad = new Squad(_units);


            switch (Type)
            {
                case FormationType.Square:
                    _formation = new SquareFormation(_squad, 5f / 2f);
                    break;
                case FormationType.Circle:
                    _formation = new CircleFormation(_squad);
                    break;
                case FormationType.Wing:
                    _formation = new WingFormation(_squad);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _formation.Position = transform.position;
            _formation.Rotation = transform.rotation.eulerAngles.y;
            _formation.Scale = transform.localScale.x;

            if (TPUnits)
            {
                TPUnits = false;

                foreach (var unit in _squad.Units)
                {
                    unit.transform.position =
                        _formation.GetUnitsPosition(_formation.GetFormationCenterPosition())[unit];
                }
            }
        }

        private void Update()
        {
            _formation.Position = transform.position;
            _formation.Rotation = transform.rotation.eulerAngles.y;
            _formation.Scale = transform.localScale.x;
            
            Vector3 squadAveragePosition3D = GameUtility.ToVec3(_squad.GetAveragePosition());

            if (Input.GetKeyDown(KeyCode.Z))
            {

                
                foreach (var unit in _squad.Units)
                {
                    unit.AddTaskGoTo(_formation.GetUnitsPosition(squadAveragePosition3D)[unit]);
                }
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                Vector3 diff = _formation.GetFormationCenterPosition() - squadAveragePosition3D;
                float sign = (diff.z < 0f) ? -1f : 1f;

                transform.rotation = Quaternion.LookRotation(-diff);

                foreach (var unit in _squad.Units)
                {
                    unit.AddTaskGoTo(_formation.GetUnitsPosition(_formation.GetFormationCenterPosition())[unit]);
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                _squad.NormalizeSquadSpeed();
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 squadAveragePosition3D = GameUtility.ToVec3(_squad.GetAveragePosition());

            Gizmos.color = Color.green;
            foreach (var position in _formation.GetUnitsPosition(squadAveragePosition3D).Values)
            {
                Gizmos.DrawSphere(squadAveragePosition3D, 0.5f);
                Gizmos.DrawWireSphere(position, 0.5f);

                Gizmos.DrawLine(position, squadAveragePosition3D);
            }

            Gizmos.color = Color.red;
            foreach (var position in _formation.GetUnitsPosition(_formation.GetFormationCenterPosition()).Values)
            {
                Gizmos.DrawSphere(_formation.GetFormationCenterPosition(), 0.5f);
                Gizmos.DrawWireSphere(position, 0.5f);
                Gizmos.DrawLine(position, _formation.GetFormationCenterPosition());
            }
        }
    }
}