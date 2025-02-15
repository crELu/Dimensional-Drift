using System.Collections.Generic;
using Enemies.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct EnemyProjectile : IComponentData
{
    public float Speed;
}
