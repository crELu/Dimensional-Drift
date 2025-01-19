using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public interface IEnemy
{
    
}
public struct EnemyHealth : IComponentData, IEnemy
{
    public float Health, MaxHealth;
}