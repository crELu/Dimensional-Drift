using System;
using Unity.Entities.Hybrid.Baking;
using UnityEngine;

namespace Enemies.Systems
{
    [RequireComponent(typeof(BakingOnlyEntityAuthoring))]
    public class EnemyGunTag: MonoBehaviour
    {
        public bool debug = true;
        private void OnDrawGizmos()
        {
            if (debug) Gizmos.DrawRay(transform.position, transform.forward * 5);
        }
    }
}