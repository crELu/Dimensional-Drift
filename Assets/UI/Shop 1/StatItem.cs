using UnityEngine;

[CreateAssetMenu(fileName = "Item Stats", menuName = "Items/Stats")]
public class StatItem : Item
{
    public AllStats stats;
    [SpritePreview] public Sprite icon;
    [TextArea(2, 8)] public string textT1;
    public override string Description => textT1;
    public override Sprite Icon => icon;
    public override void DoAction()
    {
        PlayerStats.main.augments.Add(icon);
        PlayerManager.main.AddStats(stats);
    }
}