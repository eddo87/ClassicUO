using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Stub for auto-loot functionality used by the GridHighLight system.
    /// Currently provides the minimal API surface needed for highlight-based auto-looting.
    /// Can be expanded with a full implementation later.
    /// </summary>
    internal class AutoLootManager
    {
        private static AutoLootManager _instance;
        public static AutoLootManager Instance => _instance ??= new AutoLootManager();

        /// <summary>
        /// Queue an item for auto-looting.
        /// </summary>
        public void LootItem(Item item, AutoLootConfigEntry entry = null)
        {
            // Stub: auto-loot not yet fully implemented in ClassicUO.
            // The highlight system will mark items with ShouldAutoLoot = true,
            // which can be acted on by a future full auto-loot implementation.
        }

        /// <summary>
        /// Add an auto-loot entry by graphic, hue, and name. Stub.
        /// </summary>
        public void AddAutoLootEntry(ushort graphic, ushort hue, string name)
        {
            // Stub: not yet implemented in ClassicUO.
        }

        /// <summary>
        /// Force-loot the contents of a container. Stub.
        /// </summary>
        public void ForceLootContainer(uint serial)
        {
            // Stub: not yet implemented in ClassicUO.
        }

        public class AutoLootConfigEntry
        {
            public uint DestinationContainer { get; set; }
        }
    }
}
