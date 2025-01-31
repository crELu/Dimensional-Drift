using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


public class PlayerWeapon : MonoBehaviour
{
    /// <summary>
    /// A NativeArray of bullets, sorted by increasing time
    /// </summary>
    public NativeArray<Bullet> Bullets;

    [SerializeField] protected WeaponStats baseStats;
    protected WeaponStats Stats;
    
    protected HashSet<IBasicAugment> BasicAugments = new();
    protected HashSet<ISpecializedAugment> SpecializedAugments = new();
    protected ICoreAugment CoreAugment;

    public float Cd => 1 / Stats.attackSpeed;
    public virtual void Compile()
    {
        throw new NotImplementedException();
    }

    protected AllStats CollectStats()
    {
        AllStats s = new();
        if (CoreAugment != null) s += CoreAugment.GetStats();
        foreach (var augment in BasicAugments)
        {
            s += augment.GetStats();
        }
        foreach (var augment in SpecializedAugments)
        {
            s += augment.GetStats();
        }

        return s;
    }
}

[Serializable]
public struct Bullet
{
    public Vector3 position;
    public Quaternion rotation;
    public float speed, lifetime;
    // delay until firing
    public float time;
}

