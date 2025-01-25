using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[RequireComponent(typeof(AuthorManager))]
public class BaseAuthor: MonoBehaviour
{
    public virtual void Bake(UniversalBaker baker, Entity entity)
    {
    }
}