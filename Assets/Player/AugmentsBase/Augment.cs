
// Base class

using System;
using System.Collections.Generic;
using UnityEngine;

public class Augment: ScriptableObject
{
    public virtual string Id => throw new NotImplementedException();
    public virtual int MaxStacks => throw new NotImplementedException();
    [HideInInspector] public Sprite icon;
    [field:SerializeField, Range(1, 3)]public int Stacks { get; set; }
    [field:SerializeField] public List<Requirement> Requirements { get; private set; }
    public virtual AugmentType Target => throw new NotImplementedException();
    public virtual AllStats GetStats(AllStats prevStats)
    {
        throw new NotImplementedException();
    }

    public virtual bool Add()
    {
        if (Stacks < MaxStacks - 1)
        {
            Stacks++;
        }

        return false;
    }
    
    public virtual bool Verify()
    {
        if (Stacks > MaxStacks || Stacks < 1)
        {
            Debug.Log($"Can't have that many stacks: {Stacks}");
            return false;
        }
        return true;
    }
}

[Serializable]
public struct Requirement
{
    public Augment augment;
    public int minTier;
    public bool exclusion;
}

public enum AugmentType
{
    Gun,
    Cannon,
    Laser,
    Sword,
    Character
}