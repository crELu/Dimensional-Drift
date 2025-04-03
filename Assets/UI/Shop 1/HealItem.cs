using UnityEngine;

[CreateAssetMenu(fileName = "Item Heal", menuName = "Items/Heal")]
public class HealItem : Item
{
    [SpritePreview] public Sprite icon;
    [TextArea(2, 8)] public string textT1;
    public float healing;
    public override string Description => textT1;
    public override Sprite Icon => icon;
    public override void DoAction()
    {
        PlayerManager.main.Heal(healing);
    }
}