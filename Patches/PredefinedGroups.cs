using AzuCraftyBoxes.Util.Functions;

namespace AzuCraftyBoxes.Patches;

[HarmonyPatch(typeof(ObjectDB), "Awake")]
static class PredefinedGroupGrab
{
    static void Postfix(ObjectDB __instance)
    {
        if (!ZNetScene.instance)
            return;
        MiscFunctions.CreatePredefinedGroups(__instance);
    }
}