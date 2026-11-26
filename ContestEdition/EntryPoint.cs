using BepInEx;
using BepInEx.Unity.IL2CPP;
using ChainedPuzzles;
using Enemies;
using GameData;
using GTFO.API;
using HarmonyLib;
using System;

namespace ContestEdition
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.0.0")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "ContestEdition";

        public override void Load()
        {
            EnemyPrefabManager.add_OnEnemiesGenerated((Action)(() =>
            {
                foreach (var enemy in EnemyPrefabManager.Current.m_enemyPrefabs.Values)
                {
                    var shooters = enemy.GetComponentsInChildren<EAB_ProjectileShooter>();
                    if (shooters.Count == 0) continue;

                    foreach (var eab in shooters)
                    {
                        eab.m_burstDelay *= 0.2f;
                        eab.m_shotDelayMax *= 0.2f;
                        eab.m_shotDelayMin *= 0.2f;
                    }
                }
            }));

            GameDataAPI.OnGameDataInitialized += () =>
            {
                foreach (var block in EnemyDataBlock.GetAllBlocks())
                {
                    foreach (var ability in block.AI_Abilities)
                    {
                        if (ability.AbilityPrefab.Contains("Projectile"))
                            ability.Cooldown *= 0.2f;
                    }
                }

                foreach (var block in EnemyBalancingDataBlock.GetAllBlocks())
                {
                    block.EnemyCollisionRadius *= 8f;
                    block.EnemyCollisionPlayerMovementReduction = 0.5f;
                    block.EnemyCollisionMinimumMoveSpeedModifier = 0.2f;
                }

                foreach (var block in LevelLayoutDataBlock.GetAllBlocks())
                {
                    if (block.Zones.Count == 0) continue;

                    var zone = block.Zones[0];
                    if (zone.ChainedPuzzleToEnter == 0)
                        zone.ChainedPuzzleToEnter = 4;
                }

                uint[] bannedIDs = new uint[] { 6, 42, 43, 45, 47, 51, 56, 61 };
                foreach (var id in bannedIDs)
                {
                    var block = PlayerOfflineGearDataBlock.GetBlock(id);
                    if (block != null)
                        block.internalEnabled = false;
                }

                var playerBlock = PlayerDataBlock.GetBlock(1u);
                if (playerBlock != null)
                {
                    playerBlock.runMoveSpeed -= 1;
                    playerBlock.StaminaRegenNotRestingInCombat = 0;
                    playerBlock.StaminaRegenNotRestingOutOfCombat = 0;
                    playerBlock.StaminaRegenRestingInCombat = 0;
                    playerBlock.StaminaRegenRestingOutOfCombat = 0;
                    playerBlock.breathingStaminaEnabled = false;
                }
            };
            new Harmony(MODNAME).PatchAll();
            Log.LogMessage("Loaded " + MODNAME);
        }

        [HarmonyPatch]
        static class OnLoadedPatches
        {
            [HarmonyPatch(typeof(ProjectileManager), nameof(ProjectileManager.LoadAssets))]
            [HarmonyPostfix]
            private static void OnProjectilesLoaded(ProjectileManager __instance)
            {
                foreach (var prefab in __instance.m_projectilePrefabs)
                {
                    var projectile = prefab.GetComponent<ProjectileTargeting>();
                    if (projectile == null) continue;

                    projectile.Speed *= 5f;
                    projectile.TargetStrength *= 3.3f;
                    projectile.m_targetingDelay *= 0.2f;
                    projectile.m_initialTargetStrength *= 3.3f;
                    projectile.m_initialTargetingDuration *= 0.2f;
                }
            }


            [HarmonyPatch(typeof(ChainedPuzzleManager), nameof(ChainedPuzzleManager.OnAssetsLoaded))]
            [HarmonyPostfix]
            internal static void Postfix(ChainedPuzzleManager __instance)
            {
                foreach (var prefab in __instance.m_puzzleComponentPrefabs.Values)
                {
                    var scanner = prefab.GetComponent<CP_PlayerScanner>();
                    if (scanner != null)
                    {
                        for (int i = 1; i < scanner.m_scanSpeeds.Count; i++)
                            scanner.m_scanSpeeds[i] = scanner.m_scanSpeeds[0];
                    }
                }
            }
        }
    }
}