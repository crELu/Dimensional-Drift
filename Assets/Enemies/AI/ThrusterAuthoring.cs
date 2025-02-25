using System;
using Unity.Entities.Hybrid.Baking;
using UnityEngine;

namespace Enemies.AI
{
    [RequireComponent(typeof(BakingOnlyEntityAuthoring))]
    public class ThrusterAuthoring: MonoBehaviour
    {
        public float maxThrust;

        public Thruster GetData()
        {
            return new Thruster() { maxThrust = maxThrust, forward = transform.forward };
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * maxThrust / 5);
        }
    }
}