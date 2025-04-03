using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave Data")]
public class Waves : ScriptableObject
{
    public List<WaveEnemy> enemies;
    public int Count => enemies.Sum(e => e.count);
}

public enum EnemyType
{
    MeleeRay,
    GunRay,
    Shell,
    Sunfish,
    Urchin,
    Worm
}

[Serializable]
public struct WaveEnemy
{
    public EnemyType type;
    public int count;
    public bool cat;
}