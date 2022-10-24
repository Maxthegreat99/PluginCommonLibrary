using System;
using DPoint = System.Drawing.Point;

namespace Terraria.Plugins.Common.Test {
  public static class TAssert {
    public static void IsObjectActive(int x, int y) {
      TAssert.IsObjectActive(x, y, true);
    }

    public static void IsObjectInactive(int x, int y) {
      TAssert.IsObjectActive(x, y, false);
    }

    public static void IsObjectActive(int x, int y, bool expectedState) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      if (!tile.active()) {
        throw new AssertException($"Assert failed. There is no tile at [{x},{y}].");
      }

      ObjectMeasureData measureData = TerrariaUtils.Tiles.MeasureObject(new DPoint(x, y));
      bool isActive = TerrariaUtils.Tiles.ObjectHasActiveState(measureData);
      if (isActive != expectedState) {
        string actualStateString;
        if (isActive)
          actualStateString = "active";
        else 
          actualStateString = "inactive";

        string expectedStateString;
        if (expectedState)
          expectedStateString = "Active";
        else
          expectedStateString = "Inactive";

        throw new AssertException(string.Format(
          "Assert failed. {0} frame for object \"{1}\" at [{2},{3}] was expected, but it is {4}.", 
          expectedStateString, TerrariaUtils.Tiles.GetBlockTypeName(tile.type, 0), x, y, actualStateString
        ));
      }
    }

    public static void IsTileActive(int x, int y) {
      TAssert.IsTileActive(x, y, true);
    }

    public static void IsTileInactive(int x, int y) {
      TAssert.IsTileActive(x, y, false);
    }

    public static void IsTileActive(int x, int y, bool expectedState) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      if (tile.active() != expectedState) {
        string actualStateString;
        if (tile.active())
          actualStateString = "active";
        else 
          actualStateString = "inactive";

        string expectedStateString;
        if (expectedState)
          expectedStateString = "Active";
        else
          expectedStateString = "Inactive";

        throw new AssertException(string.Format(
          "Assert failed. {0} state for tile at [{1},{2}] was expected, but it is {3}.", 
          expectedStateString, x, y, actualStateString
        ));
      }
    }

    public static void IsBlockType(int x, int y, int expectedBlockType) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      
      if (!tile.active()) {
        throw new AssertException(string.Format(
          "The tile id \"{0}\" was expected at [{1},{2}], but there is no tile at all.",
          TerrariaUtils.Tiles.GetBlockTypeName(expectedBlockType, 0), x, y
        ));
      }

      if (tile.type != (int)expectedBlockType) {
        throw new AssertException(string.Format(
          "The tile id \"{0}\" was expected at [{1},{2}], but it is \"{3}\".",
          TerrariaUtils.Tiles.GetBlockTypeName(expectedBlockType, 0), x, y, TerrariaUtils.Tiles.GetBlockTypeName(tile.type, 0)
        ));
      }
    }

    public static void HasLiquid(int x, int y) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      
      if (tile.liquid <= 0) {
        throw new AssertException($"The tile at [{x},{y}] was expected to have liquid, but it doesn't.");
      }
    }

    public static void HasNoLiquid(int x, int y) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      
      if (tile.liquid != 0) {
        throw new AssertException($"The tile at [{x},{y}] was expected to have no liquid, but it has.");
      }
    }

    public static void HasFullLiquid(int x, int y) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      
      if (tile.liquid < 255) {
        throw new AssertException($"The tile at [{x},{y}] was expected to have 255 liquid, but it has {tile.liquid}.");
      }
    }

    public static void HasNotFullLiquid(int x, int y) {
      ITile tile = TerrariaUtils.Tiles[x, y];
      
      if (tile.liquid <= 0) {
        throw new AssertException($"The tile at [{x},{y}] was expected to have at least 1 liquid, but it has none.");
      }

      if (tile.liquid == 255) {
        throw new AssertException($"The tile at [{x},{y}] was expected to have less than 255 liquid, but it has {tile.liquid}.");
      }
    }

    public static void AreItemsInBlockRect(
      int tileX, int tileY, int tileW, int tileH, int expectedItemType, int expectedCount, bool allowOtherNpcs = false
    ) {
      int x = tileX * TerrariaUtils.TileSize;
      int y = tileY * TerrariaUtils.TileSize;
      int r = x + (tileW * TerrariaUtils.TileSize);
      int b = y + (tileH * TerrariaUtils.TileSize);
      int count = 0;

      bool ofExpectedId = false;
      bool ofOtherIds = false;
      for (int i = 0; i < 200; i++) {
        Item item = Main.item[i];

        if (
          item.active && 
          item.position.X >= x && item.position.X <= r &&
          item.position.Y >= y && item.position.Y <= b
        ) {
          if (item.type == expectedItemType) {
            if (!ofExpectedId)
              ofExpectedId = true;

            count++;
          } else {
            if (!ofOtherIds)
              ofOtherIds = true;
          }
        }
      }

      if (ofExpectedId) {
        if (count != expectedCount) {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] contains {expectedCount} of the items, {count} were expected though.");
        }

        if (ofOtherIds && !allowOtherNpcs) {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] contains {expectedCount} of the items, though it contains other items aswell.");
        }
      } else {
        if (ofOtherIds) {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] does not contain the expected item. There are items of other types though.");
        } else {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] does not contain the expected item.");
        }
      }
    }

    public static void AreNPCsInBlockRect(
      int tileX, int tileY, int tileW, int tileH, int expectedNPCType, int expectedCount, bool allowOtherItems = false
    ) {
      int x = tileX * TerrariaUtils.TileSize;
      int y = tileY * TerrariaUtils.TileSize;
      int r = x + (tileW * TerrariaUtils.TileSize);
      int b = y + (tileH * TerrariaUtils.TileSize);
      int count = 0;

      bool ofExpectedId = false;
      bool ofOtherIds = false;
      for (int i = 0; i < 200; i++) {
        NPC npc = Main.npc[i];

        if (
          npc.active && 
          npc.position.X >= x && npc.position.X <= r &&
          npc.position.Y >= y && npc.position.Y <= b
        ) {
          if (npc.type == expectedNPCType) {
            if (!ofExpectedId)
              ofExpectedId = true;

            count++;
          } else {
            if (!ofOtherIds)
              ofOtherIds = true;
          }
        }
      }

      if (ofExpectedId) {
        if (count != expectedCount) {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] contains {expectedCount} of the npcs, {count} were expected though.");
        }

        if (ofOtherIds && !allowOtherItems) {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] contains {expectedCount} of the npcs, though it contains other npcs aswell.");
        }
      } else {
        if (ofOtherIds) {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] does not contain the expected npcs. There are npcs of other types though.");
        } else {
          throw new AssertException($"The block rectangle [{tileX},{tileY},{tileW},{tileH}] does not contain the expected npcs.");
        }
      }
    }

    public static bool ExpectException(Action action, Type expectedException) {
      try {
        action();
      } catch (Exception ex) {
        if (ex.GetType() == expectedException)
          return true;

        throw new AssertException(
          $"Asser failed. Expected exception \"{expectedException.Name}\" was not thrown, \"{ex.GetType().Name}\" was thrown instead.", ex
        );
      }

      throw new AssertException($"Asser failed. Expected exception \"{expectedException.Name}\" was not thrown.");
    }

    public static void Fail(string messageFormat, params object[] args) {
      throw new AssertException(string.Format(messageFormat, args));
    }
  }
}
