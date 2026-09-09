using AzuCraftyBoxes.IContainers;
using AzuCraftyBoxes.Util.Functions;

namespace AzuCraftyBoxes.Patches;

[HarmonyPatch(typeof(CookingStation), "RPC_AddFuel")]
static class CapFuel_CookingStationRPC_AddFuelPatch
{
    static bool Prefix(CookingStation __instance)
    {
        if (!__instance.GetComponent<ZNetView>().IsOwner()) return true;
        return AzuCraftyBoxes.Util.GameAccess.CookingStationFuel(__instance) < __instance.m_maxFuel;
    }
}

[HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
static class CookingStationOnAddFuelSwitchPatch
{
    static bool Prefix(CookingStation __instance, ref bool __result, Humanoid user, ItemDrop.ItemData item, ZNetView ___m_nview)
    {
        AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationOnAddFuelSwitchPatch) Looking for fuel");

        if (MiscFunctions.ShouldPrevent() || item != null ||
            AzuCraftyBoxes.Util.GameAccess.CookingStationFuel(__instance) > __instance.m_maxFuel - 1 ||
            (user.GetInventory().HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name) && Boxes.CanItemBePulled(Utils.GetPrefabName(__instance.gameObject), __instance.m_fuelItem.name)))
            return true;

        AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationOnAddFuelSwitchPatch) Missing fuel in player inventory");

        string fuelPrefabName = __instance.m_fuelItem.name;
        if (!Boxes.CanItemBePulled(Utils.GetPrefabName(__instance.gameObject), fuelPrefabName))
        {
            AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationOnAddFuelSwitchPatch) CookingStation is forbidden to pull {fuelPrefabName} by config");
            return true;
        }

        List<IContainer> nearbyContainers = Boxes.QueryFrame.Get(__instance, AzuCraftyBoxesPlugin.mRange.Value);

        string sharedName = __instance.m_fuelItem.m_itemData.m_shared.m_name;
        foreach (IContainer c in nearbyContainers)
        {
            if (!c.ContainsItem(sharedName, 1, out int result)) continue;
            result = Boxes.CheckAndDecrement(result);
            if (result <= 0) return true;
            if (!Boxes.CanItemBePulled(c.GetPrefabName(), fuelPrefabName))
            {
                AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationOnAddFuelSwitchPatch) Container at {c.GetPosition()} has {result} {fuelPrefabName} but it's forbidden by config");
                return true;
            }

            AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationOnAddFuelSwitchPatch) Container at {c.GetPosition()} has {result} {fuelPrefabName}, taking one");

            c.RemoveItem(sharedName, 1);
            c.Save();
            user.Message(MessageHud.MessageType.Center, "$msg_added " + sharedName);
            ___m_nview.InvokeRPC("RPC_AddFuel", Array.Empty<object>());
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(CookingStation), "FindCookableItem")]
static class CookingStationFindCookableItemPatch
{
    static void Postfix(CookingStation __instance, ref ItemDrop.ItemData __result)
    {
        AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationFindCookableItemPatch) Looking for cookable");

        if (MiscFunctions.ShouldPrevent() || __result != null ||
            (__instance.m_requireFire && !AzuCraftyBoxes.Util.GameAccess.CookingStationFireLit(__instance) || AzuCraftyBoxes.Util.GameAccess.CookingStationFreeSlot(__instance) == -1))
            return;

        AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationFindCookableItemPatch) Missing cookable in player inventory");

        List<IContainer> nearbyContainers = Boxes.QueryFrame.Get(__instance, AzuCraftyBoxesPlugin.mRange.Value);

        foreach (CookingStation.ItemConversion itemConversion in __instance.m_conversion)
        {
            string fromPrefabName = itemConversion.m_from.name;
            string sharedName = itemConversion.m_from.m_itemData.m_shared.m_name;

            if (!Boxes.CanItemBePulled(Utils.GetPrefabName(__instance.gameObject), fromPrefabName))
            {
                AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationOnAddFuelSwitchPatch) CookingStation is forbidden to pull {fromPrefabName} by config");
                continue;
            }

            foreach (IContainer c in nearbyContainers)
            {
                if (!c.ContainsItem(sharedName, 1, out int result)) continue;
                result = Boxes.CheckAndDecrement(result);
                if (result <= 0) continue;
                if (!Boxes.CanItemBePulled(c.GetPrefabName(), fromPrefabName))
                {
                    AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationFindCookableItemPatch) Container at {c.GetPosition()} has {result} {fromPrefabName} but it's forbidden by config");
                    continue;
                }

                AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogIfReleaseAndDebugEnable($"(CookingStationFindCookableItemPatch) Container at {c.GetPosition()} has {result} {fromPrefabName}, taking one");
                GameObject drop = ObjectDB.instance.GetItemPrefab(fromPrefabName);
                ItemDrop.ItemData itemData = drop.GetComponent<ItemDrop>().m_itemData.Clone();
                itemData.m_dropPrefab = drop;
                __result = itemData;

                c.RemoveItem(sharedName, 1);

                c.Save();
                return;
            }
        }
    }
}