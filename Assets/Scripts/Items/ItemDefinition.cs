using UnityEngine;

namespace Game.Items
{
    public enum ItemType
    {
        Resource,
        Tool,
        Food,
        Block
    }

    public enum ToolClass
    {
        None,
        Axe,
        Pickaxe
    }

    [CreateAssetMenu(menuName = "Game/Item Definition", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Base")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemType type;
        [SerializeField, Min(1)] private int maxStack = 99;
        [SerializeField] private Color iconColor = Color.white;
        [SerializeField] private string description;

        [Header("Food")]
        [SerializeField, Min(0)] private int foodHealAmount;

        [Header("Tool")]
        [SerializeField] private ToolClass toolClass;
        [SerializeField, Min(0)] private int toolTier;
        [SerializeField, Min(0.1f)] private float harvestSpeedMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemType Type => type;
        public int MaxStack => Mathf.Max(1, maxStack);
        public Color IconColor => iconColor;
        public string Description => description;
        public int FoodHealAmount => foodHealAmount;
        public ToolClass ToolClass => toolClass;
        public int ToolTier => toolTier;
        public float HarvestSpeedMultiplier => harvestSpeedMultiplier;

        public bool IsTool => type == ItemType.Tool && toolClass != ToolClass.None;

        public void Configure(string itemId, string itemName, ItemType item_type, int stack, Color color,
            string desc = "", int heal = 0, ToolClass tool = ToolClass.None, int tier = 0, float speed = 1f)
        {
            id = itemId;
            displayName = itemName;
            type = item_type;
            maxStack = stack;
            iconColor = color;
            description = desc;
            foodHealAmount = heal;
            toolClass = tool;
            toolTier = tier;
            harvestSpeedMultiplier = speed;
        }
    }
}
