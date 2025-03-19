
using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    
    public static SoundManager main;
    public GameObject hitClip, explodeClip;
    public SoundHash SpatialHash;
    private void Start()
    {
        main = this;
        SpatialHash = new SoundHash(5);
    }

    private void Update()
    {
        SpatialHash.Tick();
    }

    public void ProcessAudio(SfxCommand[] commands)
    {
        foreach (var command in commands)
        {
            if (command.Name == "Hit Enemy")
            {
                if (SpatialHash.PlaySound(command.Position, "Hit Enemy", .3f))
                {
                    var c = Instantiate(hitClip, command.Position, Quaternion.identity);
                    Destroy(c, 1);
                }
            }
            else if (command.Name == "Enemy Die")
            {
                if (SpatialHash.PlaySound(command.Position, "Enemy Die", .1f))
                {
                    var c = Instantiate(explodeClip, command.Position, Quaternion.identity);
                    Destroy(c, 1);
                }
            }
        }
    }
}

public class SoundHash
{
    private float CellSize; // Size of each grid cell
    private Dictionary<(Vector3Int, string), float> soundCooldowns = new Dictionary<(Vector3Int, string), float>();

    public SoundHash(float cellSize)
    {
        CellSize = cellSize;
    }
    public void Tick()
    {
        List<(Vector3Int, string)> keysToRemove = new List<(Vector3Int, string)>();
        var keys = new List<(Vector3Int, string)>(soundCooldowns.Keys);
        foreach (var key in keys)
        {
            soundCooldowns[key] -= Time.deltaTime;
            if (soundCooldowns[key] <= 0)
            {
                keysToRemove.Add(key);
            }
        }

        // Remove expired cooldowns
        foreach (var key in keysToRemove)
        {
            soundCooldowns.Remove(key);
        }
    }

    public bool PlaySound(Vector3 position, string soundName, float cooldown)
    {
        Vector3Int cell = GetCell(position);

        // Check if sound is on cooldown in this cell
        if (soundCooldowns.TryGetValue((cell, soundName), out float remainingTime) && remainingTime > 0)
        {
            return false;
        }
        
        // Start cooldown
        soundCooldowns[(cell, soundName)] = cooldown;
        return true;
    }

    private Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / CellSize),
            Mathf.FloorToInt(position.y / CellSize),
            Mathf.FloorToInt(position.z / CellSize) // Assuming XZ plane for 3D; use Y for 2D
        );
    }
}