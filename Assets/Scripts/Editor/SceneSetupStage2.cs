using System.Collections.Generic;
using Game.InventorySystem;
using Game.Items;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    public static class SceneSetupStage2
    {
        private const string ItemsFolder = "Assets/Resources/Items";

        [MenuItem("Game/Setup Stage2 Items + Inventory")]
        public static void Setup()
        {
            EnsureFolders();
            CreateAllItems();
            AssetDatabase.SaveAssets();
            WireInventoryInOpenScene();
            Debug.Log("Stage2: items created/updated, inventory wired on Player in open scene.");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ItemsFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
        }

        private static void CreateAllItems()
        {
            CreateResource("wood", "Дерево", new Color(0.45f, 0.29f, 0.13f));
            CreateResource("stone", "Камень", new Color(0.5f, 0.5f, 0.52f));
            CreateResource("coal", "Уголь", new Color(0.15f, 0.15f, 0.16f));
            CreateResource("copper", "Медь", new Color(0.75f, 0.45f, 0.25f));
            CreateResource("iron", "Железо", new Color(0.72f, 0.72f, 0.75f));
            CreateResource("silver", "Серебро", new Color(0.85f, 0.88f, 0.92f));
            CreateResource("gold", "Золото", new Color(0.95f, 0.78f, 0.2f));
            CreateResource("obsidian", "Обсидиан", new Color(0.18f, 0.1f, 0.28f));
            CreateResource("diamond", "Алмаз", new Color(0.55f, 0.9f, 0.95f));
            CreateResource("emerald", "Изумруд", new Color(0.2f, 0.8f, 0.4f));

            CreateTool("axe_wood", "Деревянный топор", ToolClass.Axe, 1, new Color(0.6f, 0.4f, 0.2f), 1.2f);
            CreateTool("axe_stone", "Каменный топор", ToolClass.Axe, 2, new Color(0.5f, 0.5f, 0.52f), 1.6f);
            CreateTool("axe_iron", "Железный топор", ToolClass.Axe, 3, new Color(0.72f, 0.72f, 0.75f), 2.2f);
            CreateTool("pick_wood", "Деревянная кирка", ToolClass.Pickaxe, 1, new Color(0.6f, 0.4f, 0.2f), 1.2f);
            CreateTool("pick_stone", "Каменная кирка", ToolClass.Pickaxe, 2, new Color(0.5f, 0.5f, 0.52f), 1.6f);
            CreateTool("pick_iron", "Железная кирка", ToolClass.Pickaxe, 3, new Color(0.72f, 0.72f, 0.75f), 2.2f);
            CreateTool("pick_diamond", "Алмазная кирка", ToolClass.Pickaxe, 4, new Color(0.55f, 0.9f, 0.95f), 3f);

            CreateFood("berries", "Ягоды", 10, new Color(0.85f, 0.15f, 0.3f));
            CreateFood("meat_raw", "Сырое мясо", 5, new Color(0.7f, 0.3f, 0.3f));
            CreateFood("meat_cooked", "Жареное мясо", 25, new Color(0.55f, 0.3f, 0.15f));
        }

        private static ItemDefinition LoadOrCreate(string id)
        {
            string path = $"{ItemsFolder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void CreateResource(string id, string displayName, Color color)
        {
            var item = LoadOrCreate(id);
            item.Configure(id, displayName, ItemType.Resource, 99, color, "Ресурс");
        }

        private static void CreateTool(string id, string displayName, ToolClass toolClass, int tier, Color color, float speed)
        {
            var item = LoadOrCreate(id);
            item.Configure(id, displayName, ItemType.Tool, 1, color, "Инструмент", 0, toolClass, tier, speed);
        }

        private static void CreateFood(string id, string displayName, int heal, Color color)
        {
            var item = LoadOrCreate(id);
            item.Configure(id, displayName, ItemType.Food, 32, color, "Еда", heal);
        }

        private static void WireInventoryInOpenScene()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("Player not found in open scene. Run 'Game/Setup Stage1 Scene' first, then Stage2 again.");
                return;
            }

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null)
                inventory = player.AddComponent<PlayerInventory>();

            var so = new SerializedObject(inventory);
            var startingProp = so.FindProperty("startingItems");
            startingProp.arraySize = 5;
            SetStarting(startingProp.GetArrayElementAtIndex(0), "axe_wood", 1);
            SetStarting(startingProp.GetArrayElementAtIndex(1), "pick_wood", 1);
            SetStarting(startingProp.GetArrayElementAtIndex(2), "wood", 20);
            SetStarting(startingProp.GetArrayElementAtIndex(3), "stone", 10);
            SetStarting(startingProp.GetArrayElementAtIndex(4), "berries", 5);
            so.ApplyModifiedProperties();

            var uiHost = GameObject.Find("GameSystems");
            if (uiHost == null)
                uiHost = new GameObject("GameSystems");

            var inventoryUI = uiHost.GetComponent<InventoryUI>();
            if (inventoryUI == null)
                inventoryUI = uiHost.AddComponent<InventoryUI>();

            inventoryUI.Bind(inventory);
            EditorUtility.SetDirty(inventoryUI);
        }

        private static void SetStarting(SerializedProperty prop, string id, int count)
        {
            prop.FindPropertyRelative("ItemId").stringValue = id;
            prop.FindPropertyRelative("Count").intValue = count;
        }
    }
}
