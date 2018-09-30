using System;
using System.Reflection;
using BattleTech;
using Harmony;
using UnityEngine;

namespace WeaponRealizer
{
    [HarmonyPatch(typeof(AttackDirector.AttackSequence), "GetIndividualHits")]
    static class ClusteredShotEnabler
    {
        internal const string CLUSTER_TAG = "wr-clustered_shots";
        private static FastInvokeHandler AttackSequenceGetClusteredHits;

        static bool Prepare()
        {
            if (!Core.ModSettings.ClusteredBallistics) return false;
            BuildAttackSequenceGetClusteredHits();
            return true;
        }

        private static void BuildAttackSequenceGetClusteredHits()
        {
            var mi = AccessTools.Method(typeof(AttackDirector.AttackSequence), "GetClusteredHits");
            AttackSequenceGetClusteredHits = MethodInvoker.GetHandler(mi);
        }

        static bool Prefix(ref WeaponHitInfo hitInfo, int groupIdx, int weaponIdx, Weapon weapon, float toHitChance,
            float prevDodgedDamage, AttackDirector.AttackSequence __instance)
        {
            if (!weapon.weaponDef.ComponentTags.Contains(CLUSTER_TAG)) return true;
            Logger.Debug("had the cluster tag");
            var newNumberOfShots = weapon.ProjectilesPerShot;
            var originalNumberOfShots = hitInfo.numberOfShots;
            hitInfo.numberOfShots = newNumberOfShots;
            hitInfo.toHitRolls = new float[newNumberOfShots];
            hitInfo.locationRolls = new float[newNumberOfShots];
            hitInfo.dodgeRolls = new float[newNumberOfShots];
            hitInfo.dodgeSuccesses = new bool[newNumberOfShots];
            hitInfo.hitLocations = new int[newNumberOfShots];
            hitInfo.hitPositions = new Vector3[newNumberOfShots];
            hitInfo.hitVariance = new int[newNumberOfShots];
            hitInfo.hitQualities = new AttackImpactQuality[newNumberOfShots];
            AttackSequenceGetClusteredHits.Invoke(
                __instance,
                new object[] {hitInfo, groupIdx, weaponIdx, weapon, toHitChance, prevDodgedDamage}
            );
            hitInfo.numberOfShots = originalNumberOfShots;

            PrintHitLocations(hitInfo);
            return false;
        }

        private static void PrintHitLocations(WeaponHitInfo hitInfo)
        {
            if (!Core.ModSettings.debug) return;
            try
            {
                var output = "";
                output += $"clustered hits: {hitInfo.hitLocations.Length}\n";
                for (int i = 0; i < hitInfo.hitLocations.Length; i++)
                {
                    int location = hitInfo.hitLocations[i];
                    var chassisLocationFromArmorLocation =
                        MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation) location);

                    if (location == 0 || location == 65536)
                    {
                        output += $"hitLocation {i}: NONE/INVALID\n";
                    }
                    else
                    {
                        output += $"hitLocation {i}: {chassisLocationFromArmorLocation} ({location})\n";
                    }
                }
                Logger.Debug(output);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}