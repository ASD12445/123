using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    public static class ItemRegistry
    {
        private static Dictionary<string, ItemDefinition> _byId;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized && _byId != null)
                return;

            _byId = new Dictionary<string, ItemDefinition>();
            var items = Resources.LoadAll<ItemDefinition>("Items");
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                _byId[item.Id] = item;
            }

            _initialized = true;
        }

        public static ItemDefinition Get(string id)
        {
            Initialize();
            if (string.IsNullOrEmpty(id))
                return null;
            _byId.TryGetValue(id, out var item);
            return item;
        }

        public static bool TryGet(string id, out ItemDefinition item)
        {
            Initialize();
            item = Get(id);
            return item != null;
        }

        public static IReadOnlyCollection<ItemDefinition> GetAll()
        {
            Initialize();
            return _byId.Values;
        }
    }
}
