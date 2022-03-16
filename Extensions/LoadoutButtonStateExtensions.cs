using AmadarePlugin.Features.Loadouts.UI.Behaviors;

namespace AmadarePlugin.Extensions;

public static class LoadoutButtonStateExtensions
{
    public static bool IsOccupied(this LoadoutButtonState state)
    {
        return state is LoadoutButtonState.Equipped or LoadoutButtonState.Filled or LoadoutButtonState.Unavailable;
    }
}