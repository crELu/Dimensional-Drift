using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int maxWeaponSlots = 4;
    [SerializeField] private TextMeshProUGUI intelText;
    [SerializeField] private TextMeshProUGUI waveText;
    public float Intel { get; private set; }

    private void Start()
    {
        Intel = 100;
        InitializeDefaultWeapons();
        UpdateUI();
    }

    private void InitializeDefaultWeapons()
    {
        // Placeholder for default weapon initialization
    }

    public void AddIntel(float amount) { Intel += amount; UpdateUI(); }
    public bool SpendIntel(float amount)
    {
        if (Intel >= amount)
        {
            Intel -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void UpdateUI()
    {
        intelText.text = $"{Intel:F0}";
        waveText.text = $"{PlayerManager.waveCount:F0}";
    }
}