using AzuCraftyBoxes.Compatibility.WardIsLove;
using AzuCraftyBoxes.Util.Functions;
using static AzuCraftyBoxes.Util.Functions.MiscFunctions;

namespace AzuCraftyBoxes.Patches;

[HarmonyPatch(typeof(Container), "Awake")]
internal static class ContainerAwakePatch
{
    private static void Postfix(Container __instance)
    {
        if (ShouldSkipContainer(__instance)) return;

        try
        {
            var parentPlayer = __instance.GetComponentInParent<Player>();
            if (parentPlayer != null && parentPlayer != Player.m_localPlayer)
            {
                return;
            }

            if (HasAccessToContainer(__instance))
            {
                Boxes.AddContainer(__instance);
            }
        }
        catch
        {
            //ignored TODO: Fix this for real later.
        }
    }
}

[HarmonyPatch(typeof(Container), "Load")]
static class ContainerLoadPatch
{
    static void Postfix(Container __instance)
    {
        if (ShouldSkipContainer(__instance)) return;

        Player player = Player.m_localPlayer;
        if (player == null) return;
        var parentPlayer = __instance.GetComponentInParent<Player>();
        if (parentPlayer != null && parentPlayer != player)
        {
            return;
        }

        if (AzuCraftyBoxes.Util.GameAccess.PlayerIsLoading(player) || AzuCraftyBoxes.Util.GameAccess.PlayerTeleporting(player)) return;

        if (HasAccessToContainer(__instance))
        {
            Boxes.AddContainer(__instance);
        }
    }
}

[HarmonyPatch(typeof(Container), "OnDestroyed")]
internal static class ContainerOnDestroyedPatch
{
    private static void Postfix(Container __instance)
    {
        if (ShouldSkipContainer(__instance)) return;

        Boxes.RemoveContainer(__instance);
    }
}

[HarmonyPatch(typeof(WearNTear), "OnDestroy")]
static class WearNTearOnDestroyPatch
{
    static void Prefix(WearNTear __instance)
    {
        if (ShouldPrevent()) return;

        Container[]? container = __instance.GetComponentsInChildren<Container>();
        Container[]? parentContainer = __instance.GetComponentsInParent<Container>();
        if (container.Length > 0)
        {
            foreach (Container c in container)
            {
                Boxes.RemoveContainer(c);
            }
        }

        if (parentContainer.Length <= 0) return;
        {
            foreach (Container c in parentContainer)
            {
                Boxes.RemoveContainer(c);
            }
        }
    }
}

[HarmonyPatch(typeof(Player), "UpdateTeleport")]
public static class PlayerUpdateTeleportPatchCleanupContainers
{
    public static void Prefix(float dt)
    {
        if (ShouldPrevent()) return;

        if (!(Player.m_localPlayer != null) || !AzuCraftyBoxes.Util.GameAccess.PlayerTeleporting(Player.m_localPlayer))
            return;
        foreach (Container container in Boxes.Containers.ToList().Where(container => (!(container != null) || !(container.transform != null)
                     ? 0
                     : (container.GetInventory() != null ? 1 : 0)) == 0).Where(container => container != null))
        {
            Boxes.RemoveContainer(container);
        }
    }
}