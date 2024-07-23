using Comfort.Common;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.MovingPlatforms;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Ragfair;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using UnityEngine;
using UnityEngine.UI;
using static EFT.Interactive.WindowBreakingConfig;

namespace AutoDeposit
{
    public class AutoDepositPanel : MonoBehaviour
    {
        private Button button;
        private LootItemClass container;
        private InventoryControllerClass inventoryController;

        public void Awake()
        {
            var layoutElement = GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            layoutElement.minWidth = -1f;

            var horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleRight;
            horizontalLayoutGroup.spacing = -7;
            horizontalLayoutGroup.padding = new RectOffset();
            horizontalLayoutGroup.childControlWidth = false;

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector3.one;
            rectTransform.anchorMax = Vector3.one;
            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x + 5f, rectTransform.offsetMin.y);
            rectTransform.anchoredPosition = new Vector2(0, rectTransform.rect.height + 4f);

            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }
        public static IEnumerable<Item> GetWeaponNonVitalMods(Item item)
        {
            if (!(item is Weapon weapon))
            {
                return new Collection<Item>();
            }

            var mods = weapon.Mods;
            var vitalItems = weapon.VitalParts
                                    .Select(slot => slot.ContainedItem) // get all vital items from their respective slot
                                    .Where(contained => contained != null); // only keep those not null

            var nonVitalMods = mods.Where(mod => !vitalItems.Contains(mod));
            return nonVitalMods;
        }


        public void Show(LootItemClass container)
        {
            this.container = container;
            this.inventoryController = ItemUiContext.Instance.R().InventoryController;
            gameObject.SetActive(true);
        }

        public void OnDestroy()
        {
            this.container = null;
            this.inventoryController = null;
        }

        private void OnClick()
        {
            StashClass stash = inventoryController.Inventory.Stash;
            if (stash == null)
            {
                return;
            }

            List<Item> stashItems = stash.GetNotMergedItems().ToList();

            //way1
            //Check key
            if (!Input.GetKey(KeyCode.LeftAlt) & !Input.GetKey(KeyCode.LeftShift) & !Input.GetKey(KeyCode.LeftControl) & !Input.GetKey(KeyCode.D))
            {

                 List<Item> items = container.GetNotMergedItems().Reverse().ToList(); // Reverse so items get moved before their container
                foreach (Item item in items)
                {
                    // Skip root
                    if (item == container)
                    {
                        continue;
                    }

                    // Don't move containers that aren't empty
                    if (item is LootItemClass lootItem && lootItem.GetNotMergedItems().Any(i => i != lootItem))
                    {
                        continue;
                    }

                    List<LootItemClass> targets = [];

                    foreach (var match in stashItems.Where(i => i.TemplateId == item.TemplateId))
                    {
                        var targetContainer = match.Parent.Container.ParentItem as LootItemClass;
                        if (targetContainer != stash)
                        {
                            targets.Add(targetContainer);
                        }
                    }

                    if (!targets.Any())
                    {
                        continue;
                    }

                    var result = InteractionsHandlerClass.QuickFindAppropriatePlace(item, inventoryController, targets, InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide | InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent, true);
                    if (result.Failed || !inventoryController.CanExecute(result.Value))
                    {
                        string text = result.Error is InventoryError inventoryError ? inventoryError.GetLocalizedDescription() : result.Error.ToString();
                        NotificationManagerClass.DisplayWarningNotification(text.Localized(), ENotificationDurationType.Default);

                        continue;
                    }

                    if (result.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                    {
                        NotificationManagerClass.DisplayWarningNotification(new GClass3320(item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                        continue;
                    }

                    inventoryController.RunNetworkTransaction(result.Value, null);
                }
            }
            //way1 end

            //way2
            //**Add LeftShift key modificator**
            //-First,  LeftShift + Click  => move everything of default logic, other item move to stash root without containers(for example, pull the cartridge out of the chamber, pull everything out of the containers, but leave the parent containers in the inventory).
            // - Second, LeftShift + Click => move empty any "containers" to root stash.

            //Check key
            if (Input.GetKey(KeyCode.LeftShift))
            {

                List<Item> items = container.GetAllItems().ToList(); //Here No Reverse
                foreach (Item item in items)
                {
                    // Skip root
                    if (item == container)
                    {
                        continue;
                    }

                    // Don't move containers that aren't empty
                    if (item is LootItemClass lootItem && lootItem.GetAllItems().Any(i => i != lootItem))
                    {
                        continue;
                    }

                    List<LootItemClass> targets = [];

                    foreach (var match in stashItems.Where(i => i.TemplateId == item.TemplateId))
                    {
                        var targetContainer = match.Parent.Container.ParentItem as LootItemClass;
                        if (targetContainer != stash)
                        {
                            targets.Add(targetContainer);
                        }
                    }
                    //add stash to targets
                    
                    targets.Add(stash);
                    

                    if (!targets.Any())
                    {
                        continue;
                    }

                    var result = InteractionsHandlerClass.QuickFindAppropriatePlace(item, inventoryController, targets, InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide | InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent, true);
                    if (result.Failed || !inventoryController.CanExecute(result.Value))
                    {
                        string text = result.Error is InventoryError inventoryError ? inventoryError.GetLocalizedDescription() : result.Error.ToString();
                        NotificationManagerClass.DisplayWarningNotification(text.Localized(), ENotificationDurationType.Default);

                        continue;
                    }

                    if (result.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                    {
                        NotificationManagerClass.DisplayWarningNotification(new GClass3320(item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                        continue;
                    }

                    inventoryController.RunNetworkTransaction(result.Value, null);

                }

            }
            //way2 end


            //way3
            //**Add LeftControl key modificator**
            //LeftControl + click => move all items to root stash, and at the same time, pull everything out of the containers to root stash

            //Check key
            if (Input.GetKey(KeyCode.LeftControl))
            {

                List<Item> items = container.GetAllItems().Reverse().ToList(); // Reverse so items get moved before their container
                foreach (Item item in items)
                {
                    // Skip root
                    if (item == container)
                    {
                        continue;
                    }

                    // Don't move containers that aren't empty
                    if (item is LootItemClass lootItem && lootItem.GetAllItems().Any(i => i != lootItem))
                    {
                        continue;
                    }

                    List<LootItemClass> targets = [stash];
                    
                    if (!targets.Any())
                    {
                        continue;
                    }

                    var result = InteractionsHandlerClass.QuickFindAppropriatePlace(item, inventoryController, targets, InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide | InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent, true);
                    if (result.Failed || !inventoryController.CanExecute(result.Value))
                    {
                        string text = result.Error is InventoryError inventoryError ? inventoryError.GetLocalizedDescription() : result.Error.ToString();
                        NotificationManagerClass.DisplayWarningNotification(text.Localized(), ENotificationDurationType.Default);

                        continue;
                    }

                    if (result.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                    {
                        NotificationManagerClass.DisplayWarningNotification(new GClass3320(item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                        continue;
                    }

                    inventoryController.RunNetworkTransaction(result.Value, null);

                }

            }
            //way3 end


            //way4
            //**Add LeftAlt key modificator**
            //LeftAlt + click => move all items to root stash, but don`t touch items inside any containers

            //Check key
            if (Input.GetKey(KeyCode.LeftAlt))
            {

                List<Item> items = container.GetFirstLevelItems().ToList(); // Reverse so items get moved before their container
                foreach (Item item in items)
                {

                    // Skip root
                    if (item == container)
                    {
                        continue;
                    }


                    List<LootItemClass> targets = [];

                    //add stash to targets
                    
                     targets.Add(stash);
                    

                    if (!targets.Any())
                    {
                        continue;
                    }

                    var result = InteractionsHandlerClass.QuickFindAppropriatePlace(item, inventoryController, targets, InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide | InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent, true);
                    if (result.Failed || !inventoryController.CanExecute(result.Value))
                    {
                        string text = result.Error is InventoryError inventoryError ? inventoryError.GetLocalizedDescription() : result.Error.ToString();
                        NotificationManagerClass.DisplayWarningNotification(text.Localized(), ENotificationDurationType.Default);

                        continue;
                    }

                    if (result.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                    {
                        NotificationManagerClass.DisplayWarningNotification(new GClass3320(item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                        continue;
                    }

                    inventoryController.RunNetworkTransaction(result.Value, null);

                }

            }
            //way4 end


            //way5
            //**Add D key modificator**
            //D + click => move all non vital weapons items to root stash

            //Check key
            if (Input.GetKey(KeyCode.D))
            {

                List<Item> items = container.GetAllItems().ToList();
                foreach (Item item in items)
                {
                var nonVitalMods = GetWeaponNonVitalMods(item);

                    foreach (Item nonvitalmod in nonVitalMods)
                    {

                        List<LootItemClass> targets = [];

                        //add stash to targets
                        
                         targets.Add(stash);
                        

                        if (!targets.Any())
                        {
                            continue;
                        }

                        var result = InteractionsHandlerClass.QuickFindAppropriatePlace(nonvitalmod, inventoryController, targets, InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide | InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent, true);
                        if (result.Failed || !inventoryController.CanExecute(result.Value))
                        {
                            string text = result.Error is InventoryError inventoryError ? inventoryError.GetLocalizedDescription() : result.Error.ToString();
                            NotificationManagerClass.DisplayWarningNotification(text.Localized(), ENotificationDurationType.Default);

                            continue;
                        }

                        if (result.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                        {
                            NotificationManagerClass.DisplayWarningNotification(new GClass3320(item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                            continue;
                        }

                        inventoryController.RunNetworkTransaction(result.Value, null);
                    }
                }

            }
            //way5 end

            Singleton<GUISounds>.Instance.PlayItemSound(stash.ItemSound, EInventorySoundType.pickup, false);
        }

        public static AutoDepositPanel Create(Transform parent)
        {
            GameObject template = ItemUiContext.Instance.R().GridWindowTemplate.R().GridSortPanel.gameObject;
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = "AutoDeposit";

            var gridSortPanel = clone.GetComponent<GridSortPanel>();
            UnityEngine.Object.Destroy(gridSortPanel);

            var text = clone.transform.Find("Text").gameObject;
            UnityEngine.Object.Destroy(text);

            Transform iconTransform = clone.transform.Find("SortIcon");
            iconTransform.name = "ArrowIcon";

            Transform iconTransform2 = UnityEngine.Object.Instantiate(iconTransform, clone.transform, false);
            iconTransform2.name = "BagIcon";

            Image arrowIcon = iconTransform.GetComponent<Image>();
            arrowIcon.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(EItemAttributeId.EffectiveDist);
            arrowIcon.transform.Rotate(0f, 0f, -45f);
            arrowIcon.overrideSprite = null;
            arrowIcon.SetNativeSize();

            Image bagIcon = iconTransform2.GetComponent<Image>();
            RagFairClass.IconsLoader.GetIcon("/files/handbook/icon_gear_cases.png", sprite =>
            {
                bagIcon.sprite = sprite;
                bagIcon.overrideSprite = null;
                bagIcon.SetNativeSize();
            });

            var button = clone.GetComponent<Button>();
            button.onClick.RemoveAllListeners();

            return clone.AddComponent<AutoDepositPanel>();
        }
    }
}
