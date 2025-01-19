
using UnityEngine;

[CreateAssetMenu]
public class GunEnemyAInfo : GunEnemyBaseInfo
{
    public float homing;
}

[CreateAssetMenu]
public class GunEnemyBInfo : GunEnemyBaseInfo
{
}

public class GunEnemyBaseInfo : ScriptableObject
{
    public float cd;
}