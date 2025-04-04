
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerStats: MonoBehaviour
{
    public static PlayerStats main;
    public float damageTaken, intelGained, intelMissed;
    public int enemiesKilled;
    public GameObject deathMenu;
    public TextMeshProUGUI waveText, intelText, timeText;
    public List<int> enemyTypesKilled;
    public List<Sprite> augmentRarities;
    public List<Sprite> gunAug;
    public List<Sprite> canAug;
    public List<Sprite> lasAug;
    public List<Sprite> carAug;
    public List<GameObject> gunAugSlot;
    public List<GameObject> canAugSlot;
    public List<GameObject> lasAugSlot;
    public List<GameObject> carAugSlot;
    private void Awake()
    {
        main = this;
        DontDestroyOnLoad(this);
    }

    public void UpdateUI(int wave, float time)
    {
        deathMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        PopulateStats(wave, time);
        PopulateAugments();
    }

    public void PopulateStats(int wave, float time)
    {
        waveText.text = wave.ToString();
        intelText.text = $"{intelGained:F0}";
        timeText.text = $"{Mathf.FloorToInt(time / 60)}:{Mathf.FloorToInt(time % 60)}";
    }
    
    public void PopulateAugments()
    {
        PopulateAugmentSlots(gunAug, gunAugSlot);
        PopulateAugmentSlots(canAug, canAugSlot);
        PopulateAugmentSlots(lasAug, lasAugSlot);
        PopulateAugmentSlots(carAug, carAugSlot);
    }
    
    private void PopulateAugmentSlots(List<Sprite> augments, List<GameObject> slots)
    {
        if (slots == null || augments == null || slots.Count == 0 || augments.Count == 0)
            return;

        // Count unique sprites (should be at most 3 unique ones)
        var augmentCounts = augments.GroupBy(sprite => sprite)
            .ToDictionary(group => group.Key, group => group.Count());

        int index = 0;
        foreach (var augment in augmentCounts)
        {
            if (index >= slots.Count) break;

            GameObject slot = slots[index];
            Image augmentImage = slot.GetComponent<Image>();
            Image rarityImage = slot.transform.Find("Rarity").GetComponent<Image>();

            augmentImage.sprite = augment.Key;
            rarityImage.sprite = augmentRarities[augment.Value-1];

            index++;
        }
    }
}
