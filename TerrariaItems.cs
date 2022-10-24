using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ID;
using DPoint = System.Drawing.Point;

using TShockAPI;

namespace Terraria.Plugins.Common {
  public class TerrariaItems {
    public bool IsValidType(int itemType) {
      return (itemType >= TerrariaUtils.ItemType_Min && itemType <= TerrariaUtils.ItemType_Max);
    }

    private static readonly Lazy<bool[]> craftableItemTypes = new Lazy<bool[]>(() => {
      var craftableItemTypes = new bool[TerrariaUtils.ItemType_Max + 1 + Math.Abs(TerrariaUtils.ItemType_Min)];

      for (int i = 0; i < Main.recipe.Length; i++) {
        Recipe recipe = Main.recipe[i];
        if (recipe == null)
          continue;

        int index = recipe.createItem.netID;
        if (index < 0)
          index += TerrariaUtils.ItemType_Max;

        craftableItemTypes[index] = true;
      }

      return craftableItemTypes;
    });
    public bool IsCraftableType(int itemType) {
      Check.ValidItemType(itemType);
        
      int index = itemType;
      if (index < 0)
        index += TerrariaUtils.ItemType_Max;

      return TerrariaItems.craftableItemTypes.Value[index];
    }

    private static readonly Lazy<bool[]> equipableItemTypes = new Lazy<bool[]>(() => {
      var equipableItemTypes = new bool[TerrariaUtils.ItemType_Max + 1 + Math.Abs(TerrariaUtils.ItemType_Min)];

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (
          !string.IsNullOrEmpty(dummyItem.Name) && !dummyItem.vanity && (
            dummyItem.headSlot != -1 || dummyItem.bodySlot != -1 || dummyItem.legSlot != -1 || dummyItem.accessory
          )
        ) {
          int index = i;
          if (index < 0)
            index += TerrariaUtils.ItemType_Max;

          equipableItemTypes[index] = true;
        }
      }

      return equipableItemTypes;
    });
    public bool IsEquipableType(int itemType) {
      Check.ValidItemType(itemType);

      int index = itemType;
      if (itemType < 0)
        index += TerrariaUtils.ItemType_Max;

      return TerrariaItems.equipableItemTypes.Value[index];
    }

    private static readonly Lazy<HashSet<int>> ammoTypes = new Lazy<HashSet<int>>(() => {
      var ammoTypes = new HashSet<int>();

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.ammo > 0)
          ammoTypes.Add(i);
      }
      
      return ammoTypes;
    });
    public bool IsAmmoType(int itemType) {
      Check.ValidItemType(itemType);
      return TerrariaItems.ammoTypes.Value.Contains(itemType);
    }

    private static readonly Lazy<HashSet<int>> weaponTypes = new Lazy<HashSet<int>>(() => {
      var weaponTypes = new HashSet<int>();

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.damage > 0 && dummyItem.ammo == 0)
          weaponTypes.Add(i);
      }
      
      return weaponTypes;
    });
    public bool IsWeaponType(int itemType) {
      Check.ValidItemType(itemType);
      return TerrariaItems.weaponTypes.Value.Contains(itemType);
    }

    private static readonly Lazy<HashSet<int>> accessoryTypes = new Lazy<HashSet<int>>(() => {
      var accessoryTypes = new HashSet<int>();

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.accessory)
          accessoryTypes.Add(i);
      }
      
      return accessoryTypes;
    });
    public bool IsAccessoryType(int itemType) {
      Check.ValidItemType(itemType);
      return TerrariaItems.accessoryTypes.Value.Contains(itemType);
    }

    private static readonly Lazy<HashSet<int>> vanityTypes = new Lazy<HashSet<int>>(() => {
      var vanityTypes = new HashSet<int>();

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.vanity)
          vanityTypes.Add(i);
      }

      return vanityTypes;
    });
    public bool IsVanityType(int itemType) {
      Check.ValidItemType(itemType);
      return TerrariaItems.vanityTypes.Value.Contains(itemType);
    }

    private static readonly Lazy<HashSet<int>> rareTypes = new Lazy<HashSet<int>>(() => {
      var rareTypes = new HashSet<int>();

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.rare > 0)
          rareTypes.Add(i);
      }

      return rareTypes;
    });
    public bool IsRareType(int itemType) {
      Check.ValidItemType(itemType);
      return TerrariaItems.rareTypes.Value.Contains(itemType);
    }

    private static readonly Lazy<int[][]> blockTypesItemTypes = new Lazy<int[][]>(() => {
      var blockTypesItemTypes = new int[TerrariaUtils.ItemType_Max + 1][];

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.createTile != -1) {
          int[] styleArray = blockTypesItemTypes[dummyItem.createTile];
          int[] newStyleArray;

          if (styleArray != null) {
            newStyleArray = new int[Math.Max(dummyItem.placeStyle + 1, styleArray.Length)];
            styleArray.CopyTo(newStyleArray, 0);
          } else {
            newStyleArray = new int[dummyItem.placeStyle + 1];
          }

          newStyleArray[dummyItem.placeStyle] = i;
          blockTypesItemTypes[dummyItem.createTile] = newStyleArray;
        }
      }

      return blockTypesItemTypes;
    });
    public int GetItemTypeFromBlockType(int blockType, int objectStyle = 0) {
      Check.ValidBlockType(blockType);
      
      // TODO: update and fix this
      switch (blockType) {
        case TileID.Mannequin: 
          return ItemID.Mannequin;
        case TileID.Traps:
          return ItemID.DartTrap;
        case TileID.IceBlock:
          return ItemID.None;
        default: {
          int[] styleArray = TerrariaItems.blockTypesItemTypes.Value[blockType];
          //Contract.Assert(styleArray != null, "BlockType: " + blockType);

          if (objectStyle >= styleArray.Length)
            throw new ArgumentException($"There is no item type for block \"{blockType}\" with object style {objectStyle}");

          return styleArray[objectStyle];
        }
      }
    }

    private static readonly Lazy<int[]> wallTypesItemTypes = new Lazy<int[]>(() => {
      var wallTypesItemTypes = new int[TerrariaUtils.WallType_Max + 1];

      for (int i = TerrariaUtils.ItemType_Min; i < TerrariaUtils.WallType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);
        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.createWall != -1)
          wallTypesItemTypes[dummyItem.createWall] = i;
      }

      return wallTypesItemTypes;
    });
    public int GetItemTypeFromWallType(int wallType) {
      Check.ValidWallType(wallType);
      return TerrariaItems.wallTypesItemTypes.Value[wallType];
    }

    public bool IsCoinType(int itemType) {
      switch (itemType) {
        case ItemID.CopperCoin:
        case ItemID.SilverCoin:
        case ItemID.GoldCoin:
        case ItemID.PlatinumCoin:
          return true;
      }

      return false;
    }

    public int GetHigherTierCoinType(int itemType) {
      if (itemType == ItemID.CopperCoin)
        return ItemID.SilverCoin;
      else if (itemType == ItemID.SilverCoin)
        return ItemID.GoldCoin;
      else if (itemType == ItemID.GoldCoin)
        return ItemID.PlatinumCoin;

      return -1;
    }

    public string GetItemName(ItemData itemData, bool includePrefix = false) {
      string name = "";
      if ((itemData.Prefix != 0 && includePrefix) || itemData.Type < 0)
        name += "<" + this.GetItemPrefixName(itemData.Prefix) + "> ";

      name += Lang.GetItemNameValue(itemData.Type);
      return name;
    }

    public string GetItemName(int itemType) {
      Check.ValidItemType(itemType);

      return Lang.GetItemNameValue(itemType);
    }

    public string GetItemRepresentativeString(ItemData itemData) {
      string format = "{0}";
      if (itemData.StackSize > 1)
        format = "{0} ({1})";

      return string.Format(format, this.GetItemName(itemData, true), itemData.StackSize);
    }

    public string GetItemPrefixName(int prefixType) {
      return Lang.prefix[prefixType].Value;
    }

    public int CreateNew(TSPlayer forPlayer, DPoint location, ItemData itemData, bool sendPacket = true) {
      int itemIndex = Item.NewItem(null, 
        location.X, location.Y, 0, 0, (int)itemData.Type, itemData.StackSize, true, (int)itemData.Prefix
      );

      if (sendPacket)
        forPlayer.SendData(PacketTypes.ItemDrop, string.Empty, itemIndex);

      return itemIndex;
    }

    public IEnumerable<Item> EnumerateItemsInRect(Rectangle rect) {
      int areaL = rect.Left - (rect.Width / 2);
      int areaT = rect.Top - (rect.Height / 2);
      int areaR = rect.Left + (rect.Width / 2);
      int areaB = rect.Top + (rect.Height / 2);

      for (int i = 0; i < 200; i++) {
        Item item = Main.item[i];

        if (
          item.active &&
          item.position.X > areaL && item.position.X < areaR &&
          item.position.Y > areaT && item.position.Y < areaB
        ) {
          yield return item;
        }
      }
    }

    public IEnumerable<Item> EnumerateItemsAroundPoint(DPoint location, int radiusInPixels) {
      for (int i = 0; i < 200; i++) {
        Item item = Main.item[i];

        if (
          item.active && 
          Math.Sqrt(Math.Pow(item.position.X - location.X, 2) + Math.Pow(item.position.Y - location.Y, 2)) <= radiusInPixels
        )
          yield return item;
      }
    }
  }
}
