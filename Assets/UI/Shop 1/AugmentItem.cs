using UnityEngine;

[CreateAssetMenu(fileName = "Item Augment", menuName = "Items/Augment")]
public class AugmentItem : Item
{
    public Augment augment;
    public AugmentType type;
    public ShopAugmentTarget target;

    public override void DoAction()
    {
        switch (target)
        {
            case ShopAugmentTarget.Character:
                PlayerManager.main.weapons[0].AddAugment(augment);
                break;
            default:
                PlayerManager.main.weapons[(int)target].AddAugment(augment);
                break;
        }
    }
}

public enum ShopAugmentTarget
{
    Weapon1,
    Weapon2,
    Weapon3,
    Weapon4,
    Character
}