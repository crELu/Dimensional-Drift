using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GunWeapon: PlayerWeapon
{
    public Vector3 position;
    public bool compile;
    private void Start()
    {
        Compile();
    }

    private void Update()
    {
        if (compile) Compile();
    }

    public override void Compile()
    {
        Bullets.Dispose();
        List<Bullet> bullets = new();
        Stats = baseStats * CollectStats().weaponStats;
        float weaponCd = 1 / Stats.attackSpeed;
        float attackCd = weaponCd / (2 + 2 * Stats.count);
        
        for (int i = 0; i < 1 + Stats.count; i++)
        {
            bullets.Add(new Bullet {position = position, rotation = Quaternion.identity, time = i * attackCd, speed = Stats.speed, lifetime = Stats.duration});
        }

        Bullets = new(bullets.ToArray(), Allocator.Persistent);
    }
}