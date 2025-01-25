using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Serialization;

class DamagePlayerAuthor : BaseAuthor
{
    public int damage;
    public bool dieOnHit;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new DamagePlayer
        {
            Damage = damage,
            DieOnHit = dieOnHit
        });
        base.Bake(baker, entity);
    }
}

public struct DamagePlayer : IComponentData
{
    public int Damage;
    public bool DieOnHit;
}