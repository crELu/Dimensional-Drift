using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI intelText, intelText2;
    [field:SerializeField] public float Intel { get; private set; }

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

    public void AddIntel(float amount)
    {
        Intel += amount;
        PlayerStats.main.intelGained += amount;
        UpdateUI(); 
        
    }
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
        intelText2.text = $"{Intel:F0}";
    }
}