using System.Collections.Generic;
using System.Reflection;

namespace AzuCraftyBoxes.Util;

// DeepNorth privatized a wide range of game members this mod uses.
// Public re-implementations live next to each call site where trivial;
// everything else goes through these cached reflection helpers so a
// missing member degrades to a logged no-op instead of a dead mod.
internal static class GameAccess
{
    private static MethodInfo? Method(System.Type type, string name, System.Type[]? args = null) =>
        HarmonyLib.AccessTools.Method(type, name, args);

    private static FieldInfo? Field(System.Type type, string name) =>
        HarmonyLib.AccessTools.Field(type, name);

    private static readonly MethodInfo? InventoryChangedMethod = Method(typeof(Inventory), "Changed");
    private static readonly MethodInfo? ContainerSaveMethod = Method(typeof(Container), "Save");
    private static readonly MethodInfo? ContainerCheckAccessMethod = Method(typeof(Container), "CheckAccess", new[] { typeof(long) });
    private static readonly MethodInfo? SmelterGetFuel = Method(typeof(Smelter), "GetFuel");
    private static readonly MethodInfo? SmelterGetQueueSize = Method(typeof(Smelter), "GetQueueSize");
    private static readonly MethodInfo? CookingStationGetFuel = Method(typeof(CookingStation), "GetFuel");
    private static readonly MethodInfo? CookingStationGetFreeSlot = Method(typeof(CookingStation), "GetFreeSlot");
    private static readonly MethodInfo? CookingStationIsFireLit = Method(typeof(CookingStation), "IsFireLit");
    private static readonly MethodInfo? ShieldGeneratorGetFuel = Method(typeof(ShieldGenerator), "GetFuel");
    private static readonly MethodInfo? TurretFindAmmoItem = Method(typeof(Turret), "FindAmmoItem", new[] { typeof(Inventory), typeof(bool) });
    private static readonly MethodInfo? FermenterGetStatus = Method(typeof(Fermenter), "GetStatus");
    private static readonly MethodInfo? FermenterIsItemAllowedRef = Method(typeof(Fermenter), "IsItemAllowed", new[] { typeof(ItemDrop.ItemData) });
    private static readonly MethodInfo? ObjectDBUpdateRegistersRef = Method(typeof(ObjectDB), "UpdateRegisters");
    private static readonly MethodInfo? ChatAddInworldTextRef = Method(typeof(Chat), "AddInworldText");
    private static readonly MethodInfo? PlayerTakeInputRef = Method(typeof(Player), "TakeInput");
    private static readonly MethodInfo? PlayerKnowStationLevelRef = Method(typeof(Player), "KnowStationLevel", new[] { typeof(string), typeof(int) });
    private static readonly FieldInfo? CharacterSeman = Field(typeof(Character), "m_seman");
    private static readonly FieldInfo? PlayerTeleportingRef = Field(typeof(Player), "m_teleporting");
    private static readonly FieldInfo? PlayerIsLoadingRef = Field(typeof(Player), "m_isLoading");
    private static readonly FieldInfo? PlayerKnownMaterialRef = Field(typeof(Player), "m_knownMaterial");

    private static void LogMissing(string what) =>
        AzuCraftyBoxesPlugin.AzuCraftyBoxesLogger.LogWarning($"DeepNorth port: game member not found ({what}), skipping.");

    public static void InventoryChanged(Inventory? inv)
    {
        if (inv == null || InventoryChangedMethod == null) { if (inv != null) LogMissing("Inventory.Changed"); return; }
        // Pass explicit args: Mono ignores optional defaults when args array is null.
        InventoryChangedMethod.Invoke(inv, new object[] { false, false });
    }

    public static void ContainerSave(Container container)
    {
        if (ContainerSaveMethod == null) { LogMissing("Container.Save"); return; }
        ContainerSaveMethod.Invoke(container, null);
    }

    public static bool ContainerCheckAccess(Container container, long playerId)
    {
        if (ContainerCheckAccessMethod == null) { LogMissing("Container.CheckAccess"); return true; }
        return (bool)ContainerCheckAccessMethod.Invoke(container, new object[] { playerId })!;
    }

    private static float CallFloat(MethodInfo? method, object target, string what, float fallback = 0f)
    {
        if (method == null) { LogMissing(what); return fallback; }
        return (float)method.Invoke(target, null)!;
    }

    private static int CallInt(MethodInfo? method, object target, string what, int fallback = 0)
    {
        if (method == null) { LogMissing(what); return fallback; }
        return (int)method.Invoke(target, null)!;
    }

    public static float SmelterFuel(Smelter s) => CallFloat(SmelterGetFuel, s, "Smelter.GetFuel");
    public static int SmelterQueueSize(Smelter s) => CallInt(SmelterGetQueueSize, s, "Smelter.GetQueueSize");
    public static float CookingStationFuel(CookingStation s) => CallFloat(CookingStationGetFuel, s, "CookingStation.GetFuel");
    public static int CookingStationFreeSlot(CookingStation s) => CallInt(CookingStationGetFreeSlot, s, "CookingStation.GetFreeSlot", -1);
    public static float ShieldGeneratorFuel(ShieldGenerator s) => CallFloat(ShieldGeneratorGetFuel, s, "ShieldGenerator.GetFuel");
    public static bool CookingStationFireLit(CookingStation s)
    {
        if (CookingStationIsFireLit == null) { LogMissing("CookingStation.IsFireLit"); return true; }
        return (bool)CookingStationIsFireLit.Invoke(s, null)!;
    }

    public static ItemDrop.ItemData? TurretAmmoItem(Turret t, Inventory inventory, bool onlyLoadable)
    {
        if (TurretFindAmmoItem == null) { LogMissing("Turret.FindAmmoItem"); return null; }
        return (ItemDrop.ItemData?)TurretFindAmmoItem.Invoke(t, new object[] { inventory, onlyLoadable });
    }

    public static bool FermenterIsEmpty(Fermenter f)
    {
        if (FermenterGetStatus == null) { LogMissing("Fermenter.GetStatus"); return true; }
        return FermenterGetStatus.Invoke(f, null)?.ToString() == "Empty";
    }

    public static bool FermenterIsItemAllowed(Fermenter f, ItemDrop.ItemData item)
    {
        if (FermenterIsItemAllowedRef == null) { LogMissing("Fermenter.IsItemAllowed"); return true; }
        return (bool)FermenterIsItemAllowedRef.Invoke(f, new object[] { item })!;
    }

    public static void ObjectDBUpdateRegisters(ObjectDB db)
    {
        if (ObjectDBUpdateRegistersRef == null) { LogMissing("ObjectDB.UpdateRegisters"); return; }
        ObjectDBUpdateRegistersRef.Invoke(db, null);
    }

    public static void ChatAddInworldText(GameObject go, long senderId, Vector3 position, Talker.Type type, UserInfo user, string text)
    {
        if (ChatAddInworldTextRef == null || Chat.instance == null) { LogMissing("Chat.AddInworldText"); return; }
        ChatAddInworldTextRef.Invoke(Chat.instance, new object[] { go, senderId, position, type, user, text });
    }

    public static bool PlayerTakeInput(Player player)
    {
        if (PlayerTakeInputRef == null) { LogMissing("Player.TakeInput"); return true; }
        return (bool)PlayerTakeInputRef.Invoke(player, null)!;
    }

    public static bool PlayerKnowStationLevel(Player player, string station, int level)
    {
        if (PlayerKnowStationLevelRef == null) { LogMissing("Player.KnowStationLevel"); return true; }
        return (bool)PlayerKnowStationLevelRef.Invoke(player, new object[] { station, level })!;
    }

    public static bool PlayerKnownMaterial(Player player, string sharedName)
    {
        if (PlayerKnownMaterialRef?.GetValue(player) is HashSet<string> known)
            return known.Contains(sharedName);
        LogMissing("Player.m_knownMaterial");
        return true;
    }

    public static bool PlayerTeleporting(Player player) => PlayerTeleportingRef?.GetValue(player) is true;
    public static bool PlayerIsLoading(Player player) => PlayerIsLoadingRef?.GetValue(player) is true;

    public static SEMan? GetSEMan(Character character) => CharacterSeman?.GetValue(character) as SEMan;

    // ItemDrop.GetPrefabName went private; same logic (strip " (Clone)" suffix).
    public static string ItemDropGetPrefabName(string name)
    {
        char[] anyOf = new char[2] { '(', ' ' };
        int num = name.IndexOfAny(anyOf);
        return num >= 0 ? name.Substring(0, num) : name;
    }
}
