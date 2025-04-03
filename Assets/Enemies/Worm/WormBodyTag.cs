using Enemies.AI;
using Unity.Entities;
using UnityEngine;

namespace Enemies.Worm
{
    public class WormBodyAuthor: BaseAuthor
    {
        [HideInInspector] public GameObject head;
        [HideInInspector] public GameObject prev;
        public float spacing;
        public float rollSpeed;
        [Range(0,1)] public float speed;
        public float damageMult;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity,
                new WormBody
                {
                    Prev = baker.GetEntity(prev), Head = baker.GetEntity(head), Spacing = spacing,
                    RollSpeed = rollSpeed, Speed = speed, DamageMultiplier = damageMult,
                });
            baker.AddComponent(entity, new EnemyCollisionReceiver{Size = 5});
            baker.AddComponent(entity, new EnemyGhostedTag());
            base.Bake(baker, entity);
        }
    }
}