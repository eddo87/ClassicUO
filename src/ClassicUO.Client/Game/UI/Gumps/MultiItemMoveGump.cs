using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Stub for TazUO's multi-item move feature.
    /// Provides a no-op API surface so GridContainer compiles.
    /// </summary>
    internal static class MultiItemMoveGump
    {
        public static bool TrySelect(Item item) => false;
        public static bool IsSelected(uint serial) => false;
        public static bool ToggleItem(Item item) => false;
        public static void ShowNextTo(GridContainer container) { }
    }
}
