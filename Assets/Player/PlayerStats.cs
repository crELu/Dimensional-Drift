
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStats: MonoBehaviour
{
    public static PlayerStats main;
    public float damageTaken;
    public int enemiesKilled;
    public List<int> enemyTypesKilled;
    public List<Sprite> augments;
    
    private void Awake()
    {
        main = this;
        DontDestroyOnLoad(this);
    }
}
