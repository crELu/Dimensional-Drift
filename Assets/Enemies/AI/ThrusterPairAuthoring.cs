using System;
using Unity.Entities.Hybrid.Baking;
using UnityEngine;

namespace Enemies.AI
{
    [RequireComponent(typeof(BakingOnlyEntityAuthoring))]
    public class ThrusterPairAuthoring: MonoBehaviour
    {
        public float maxThrust, angle;

        public ThrusterPair GetData()
        {
            Thruster a = new Thruster{ maxThrust = maxThrust, forward = Quaternion.AngleAxis(angle, transform.right) * transform.forward };
            Thruster b = new Thruster{ maxThrust = maxThrust, forward = Quaternion.AngleAxis(-angle, transform.right) * transform.forward };
            return new ThrusterPair{LocalAxisPrimary = transform.up, LocalAxisSecondary = transform.forward, ThrusterA = a, ThrusterB = b};
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(angle, transform.right) * transform.forward * maxThrust / 5);
            Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(-angle, transform.right) * transform.forward * maxThrust / 5);
        }
    }
}