using UnityEngine;

public class GunCoreAugment : ScriptableObject, ICoreAugment
{
    public int MaxStacks => 1;
    public int Stacks { get; set; }

    public AllStats GetStats()
    {
        throw new System.NotImplementedException();
    }
}
