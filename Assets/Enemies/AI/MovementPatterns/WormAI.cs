using Unity.Entities;
using UnityEngine;

namespace Enemies.AI
{
    class WormAI : BaseEnemyAuthor
    {
        [Header("Worm Settings")]
        public float attackRange;
        public float hideRange;
        public float spacingRange;
        public float hideHealthFactor;
        public float turnSpeed;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new SpikeBall
            {
                TargetSpeed = turnSpeed,
                SpacingRange = spacingRange,
                AttackRange = attackRange * attackRange,
                HideRange = hideRange * hideRange,
                HideHealthFactor = hideHealthFactor,
            });
            health = hideHealthFactor * health;
            base.Bake(baker, entity);
            health /= hideHealthFactor;
        }
    }
    
    public struct WormHead : IComponentData
    {
        public float TargetSpeed;
        public float SpacingRange;
        public float HideHealthFactor;
        public float AttackRange;
        public float HideRange;
        public float AnimTime;
        public bool Attacking;
    }
    
    public struct WormBody : IComponentData
    {
    }
}