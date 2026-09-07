using System.Collections.Generic;

namespace Game.Schema.State
{
    public sealed class MenuSelectionState
    {
        public MenuSelection[] Menus { get; set; }
    }

    public sealed class UnlockedFoodState
    {
        public HashSet<string> RecipeIds { get; } = new();
    }
}
