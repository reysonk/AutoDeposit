﻿using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;
using System.Runtime;
using UnityEngine;
using static EFT.SpeedTree.TreeWind;

namespace AutoDeposit
{
    public class AddTransferButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TransferItemsScreen), m => m.Name == nameof(TransferItemsScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(TransferItemsScreen __instance, GridView ____itemsToTransferGridView)
        {
            if (!Settings.EnableTransfer.Value)
            {
                return;
            }

            Transform leftGrid = __instance.transform.Find("TransferScreen/Left Person/Possessions Grid");
            AutoDepositPanel autoDepositPanel = leftGrid.Find("AutoDeposit")?.GetComponent<AutoDepositPanel>();
            if (autoDepositPanel == null)
            {
                autoDepositPanel = AutoDepositPanel.Create(leftGrid);
            }

            if (____itemsToTransferGridView.Grid.ParentItem is LootItemClass container)
            {
                autoDepositPanel.Show(container);
            }
        }
    }
}
