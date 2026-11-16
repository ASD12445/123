using Game.InventorySystem;
using Game.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;

        private Canvas _canvas;
        private RectTransform _hotbarRoot;
        private RectTransform _panelRoot;
        private GameObject[] _hotbarSlotViews;
        private GameObject[] _mainSlotViews;
        private Text _selectedLabel;
        private Font _font;

        private static readonly Color SlotBg = new Color(0.12f, 0.12f, 0.14f, 0.85f);
        private static readonly Color SlotBorder = new Color(0.4f, 0.4f, 0.45f, 0.9f);
        private static readonly Color SelectedBorder = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        public void Bind(PlayerInventory inventory)
        {
            playerInventory = inventory;
        }

        private void Start()
        {
            if (playerInventory != null)
                Attach(playerInventory);
        }

        private void Attach(PlayerInventory inventory)
        {
            playerInventory = inventory;
            if (playerInventory == null || playerInventory.Inventory == null)
                return;

            playerInventory.Inventory.Changed += RefreshAll;
            playerInventory.Inventory.SlotChanged += RefreshSlot;
            playerInventory.Inventory.HotbarSelectionChanged += _ => RefreshSelection();
            playerInventory.UiOpenChanged += OnUiOpenChanged;
            RefreshAll();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
            BuildHotbar();
            BuildMainPanel();
            _panelRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (playerInventory == null || playerInventory.Inventory == null)
                return;

            playerInventory.Inventory.Changed -= RefreshAll;
            playerInventory.Inventory.SlotChanged -= RefreshSlot;
            playerInventory.UiOpenChanged -= OnUiOpenChanged;
        }

        private void OnUiOpenChanged(bool open)
        {
            _panelRoot.gameObject.SetActive(open);
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("InventoryCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.transform.SetParent(transform, false);
                eventSystem = eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
                DestroyImmediate(oldModule);

            if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void BuildHotbar()
        {
            _hotbarRoot = CreateRect("Hotbar", _canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 18f),
                new Vector2(8f * 64f, 64f));

            _hotbarSlotViews = new GameObject[Inventory.HotbarSize];
            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                var slot = CreateSlotView(_hotbarRoot, $"Hotbar_{i}", new Vector2(i * 64f, 0f));
                _hotbarSlotViews[i] = slot;
            }

            _selectedLabel = CreateText(_hotbarRoot, "SelectedLabel", "", 20,
                new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(600f, 28f));
        }

        private void BuildMainPanel()
        {
            _panelRoot = CreateRect("InventoryPanel", _canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(8f * 64f + 40f, 4f * 64f + 90f));
            var bg = _panelRoot.gameObject.AddComponent<Image>();
            bg.color = PanelBg;

            var title = CreateText(_panelRoot, "Title", "INVENTORY  (Tab - close)", 22,
                new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(500f, 30f));

            _mainSlotViews = new GameObject[Inventory.MainSize];
            int cols = 8;
            for (int i = 0; i < Inventory.MainSize; i++)
            {
                int row = i / cols;
                int col = i % cols;
                var slot = CreateSlotView(_panelRoot, $"Main_{i}",
                    new Vector2(col * 64f, -40f - row * 64f));
                _mainSlotViews[i] = slot;
            }
        }

        private GameObject CreateSlotView(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(60f, 60f);
            rect.anchoredPosition = anchoredPos + new Vector2(30f, 30f);

            var bg = go.AddComponent<Image>();
            bg.color = SlotBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = SlotBorder;
            outline.effectDistance = new Vector2(2f, -2f);

            var icon = new GameObject("Icon");
            icon.transform.SetParent(go.transform, false);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8f, 8f);
            iconRect.offsetMax = new Vector2(-8f, -8f);
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.clear;

            var count = CreateText(go.transform, "Count", "", 16,
                new Vector2(1f, 0f), new Vector2(-12f, 10f), new Vector2(40f, 20f));
            count.alignment = TextAnchor.LowerRight;

            return go;
        }

        private Text CreateText(Transform parent, string name, string content, int size,
            Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;

            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.text = content;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private void RefreshAll()
        {
            if (playerInventory == null || playerInventory.Inventory == null)
                return;

            for (int i = 0; i < _hotbarSlotViews.Length; i++)
                RefreshSlot(i);
            for (int i = 0; i < _mainSlotViews.Length; i++)
                RefreshSlot(Inventory.HotbarSize + i);
            RefreshSelection();
        }

        private void RefreshSlot(int index)
        {
            if (playerInventory == null || playerInventory.Inventory == null)
                return;

            var stack = playerInventory.Inventory.GetSlot(index);
            GameObject view = index < Inventory.HotbarSize
                ? _hotbarSlotViews[index]
                : _mainSlotViews[index - Inventory.HotbarSize];

            if (view == null)
                return;

            var icon = view.transform.Find("Icon").GetComponent<Image>();
            var count = view.transform.Find("Count").GetComponent<Text>();

            if (stack == null || stack.IsEmpty)
            {
                icon.color = Color.clear;
                count.text = "";
                return;
            }

            var item = stack.Item;
            icon.color = item != null ? item.IconColor : Color.magenta;
            count.text = stack.Count > 1 ? stack.Count.ToString() : "";
        }

        private void RefreshSelection()
        {
            if (playerInventory == null || playerInventory.Inventory == null)
                return;

            for (int i = 0; i < _hotbarSlotViews.Length; i++)
            {
                var outline = _hotbarSlotViews[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = i == playerInventory.Inventory.SelectedHotbarIndex
                        ? SelectedBorder
                        : SlotBorder;
                }
            }

            var selected = playerInventory.Inventory.GetSelectedItem();
            _selectedLabel.text = selected != null ? selected.DisplayName : "";
        }
    }
}
