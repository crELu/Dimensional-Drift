using System;
using Unity.Entities.Hybrid.Baking;
using UnityEngine;

namespace Enemies.Systems
{
    [RequireComponent(typeof(BakingOnlyEntityAuthoring))]
    public class EnemyGunTag: MonoBehaviour
    {
        public bool debug = true;
        
        [Header("Spread Settings")]
        public int spreadCount = 1;
        public float spreadAngle = .5f;
        public float distance = 1;
        private void OnDrawGizmos()
        {
            if (debug)
            {
                for (int j = 0; j < spreadCount; j++)
                {
                    float step = spreadCount > 1 ? spreadAngle / (spreadCount - 1) : 0f;
                    float start = spreadCount > 1 ? -spreadAngle / 2f : 0f;
                    Vector3 d = transform.rotation * Quaternion.Euler(0, start + j * step, 0) * Vector3.forward;
                    Gizmos.DrawRay(transform.position + d*distance,  d * (distance + 5));
                }
            }
        }
    }
}