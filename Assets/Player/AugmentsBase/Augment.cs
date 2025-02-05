
// Base class

using System;
using System.Collections.Generic;
using UnityEngine;

public class Augment: ScriptableObject
{
    [field:SerializeField] public virtual int MaxStacks { get; private set; }
    [field:SerializeField, Range(1, 3)]public int Stacks { get; set; }
    [field:SerializeField] public List<Requirement> Requirements { get; private set; }

    public virtual AllStats GetStats()
    {
        throw new System.NotImplementedException();
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