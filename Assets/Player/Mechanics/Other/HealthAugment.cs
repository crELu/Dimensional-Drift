using UnityEngine;

public class HealthAugment: ScriptableObject, ICharacterAugment
{
    public int MaxStacks { get; }
    public int Stacks { get; set; }
    public AllStats GetStats()
    {
        return new();
    }
}
