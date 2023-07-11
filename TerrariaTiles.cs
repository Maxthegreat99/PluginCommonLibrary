using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ObjectData;
using DPoint = System.Drawing.Point;

using TShockAPI;

namespace Terraria.Plugins.Common {
  public class TerrariaTiles {
    public ITile this[int x, int y] {
      get {
        #if DEBUG
        Check.ValidTileLocation(x, y);
        #endif
        return Main.tile[x, y];
      }
    }

    public ITile this[DPoint point] {
      get {
        #if DEBUG
        Check.ValidTileLocation(point.X, point.Y);
        #endif
        return Main.tile[point.X, point.Y];
      }
    }

    public bool IsValidCoord(DPoint point) {
      return this.IsValidCoord(point.X, point.Y);
    }

    public bool IsValidCoord(int x, int y) {
      return (
        x >= 0 && x < Main.maxTilesX - 1 &&
        y >= 0 && y < Main.maxTilesY - 1
      );
    }

    /// <summary>
    ///   Tells whether the object type can be right clicked to change its state.
    /// </summary>
    public bool IsSwitchableObject(int blockType) {
      return (
        blockType == TileID.Switches ||
        blockType == TileID.Lever ||
        blockType == TileID.PressurePlates ||
        blockType == TileID.Timers ||
        blockType == TileID.MusicBoxes
      );
    }

    private static readonly Lazy<string[][]> blockTypeNames = new Lazy<string[][]>(() => {
      var blockTypeNames = new List<string>[TerrariaUtils.BlockType_Max + 1];

      for (int i = 0; i < TerrariaUtils.ItemType_Max + 1; i++) {
        Item dummyItem = new Item();
        dummyItem.netDefaults(i);

        if (!string.IsNullOrEmpty(dummyItem.Name) && dummyItem.createTile != -1) {
          List<string> styleNames = blockTypeNames[dummyItem.createTile] ?? new List<string>(10);
          while (styleNames.Count <= dummyItem.placeStyle)
            styleNames.Add(dummyItem.Name);

          styleNames[dummyItem.placeStyle] = dummyItem.Name;
          blockTypeNames[dummyItem.createTile] = styleNames;
        }
      }
      
      var result = new string[TerrariaUtils.BlockType_Max + 1][];
      for (int i = 0; i < blockTypeNames.Length; i++)
        if (blockTypeNames[i] != null)
          result[i] = blockTypeNames[i].ToArray();

      return result;
    });
    public string GetBlockTypeName(ITile tile) {
      string[] styleNames = blockTypeNames.Value[tile.type];
      if (styleNames == null)
        return "Block";

      TileObjectData objectData = TileObjectData.GetTileData(tile);
      int styleIndex = tile.frameX / objectData.CoordinateFullWidth;
      return styleNames[styleIndex];
    }
    public string GetBlockTypeName(int blockType, int style = 0) {
      string[] styleNames = blockTypeNames.Value[blockType];
      if (styleNames == null)
        return "Block";
      
      return styleNames[style];
    }

    /// <summary>
    ///   Tells whether a block type is blocking the player from passing through (includes cobwebs).
    /// </summary>
    public bool IsSolidBlockType(
      int blockType, bool considerActiveStoneSolid = false, bool considerWoodPlatformSolid = false, 
      bool considerBoulderSolid = false, bool considerDartTrapsSolid = false
    ) {
      if (blockType == TileID.ActiveStoneBlock || blockType == TileID.InactiveStoneBlock)
        return considerActiveStoneSolid;
      if (blockType == TileID.Platforms)
        return considerWoodPlatformSolid;
      if (blockType == TileID.Boulder)
        return considerBoulderSolid;
      if (blockType == TileID.Traps)
        return considerDartTrapsSolid;

      return Main.tileSolid[blockType];
    }

    public void PlantHerb(DPoint tileLocation, HerbStyle style, HerbGrowthState state = HerbGrowthState.Mature) {
      ITile tile = TerrariaUtils.Tiles[tileLocation];
      tile.active(true);
      switch (state) {
        case HerbGrowthState.Growing:
          tile.type = TileID.ImmatureHerbs;
          break;
        case HerbGrowthState.Mature:
          tile.type = TileID.MatureHerbs;
          break;
        case HerbGrowthState.Blooming:
          tile.type = TileID.BloomingHerbs;
          break;
        default:
          throw new ArgumentException("state");
      }

      tile.frameX = Convert.ToInt16((int)style * TerrariaUtils.DefaultTextureTileSize);
      TSPlayer.All.SendTileSquareCentered(tileLocation.X, tileLocation.Y, 3);
    }

    public void SetBlock(DPoint tileLocation, int blockType, bool localOnly = false, bool squareFrame = true) {
      ITile tile = TerrariaUtils.Tiles[tileLocation];

      if (blockType == -1) {
        this.RemoveBlock(tileLocation, squareFrame, localOnly);
        return;
      } else {
        tile.type = (ushort)blockType;
        tile.active(true);
      }

      if (squareFrame)
        WorldGen.SquareTileFrame(tileLocation.X, tileLocation.Y, true);
      if (!localOnly)
        TSPlayer.All.SendTileSquareCentered(tileLocation.X, tileLocation.Y, 1);
    }

    public void RemoveBlock(DPoint tileLocation, bool squareFrames = true, bool localOnly = false) {
      ITile tile = TerrariaUtils.Tiles[tileLocation];

      tile.type = 0;
      tile.active(false);
      tile.frameX = -1;
      tile.frameY = -1;
      tile.frameNumber(0);

      if (squareFrames)
        WorldGen.SquareTileFrame(tileLocation.X, tileLocation.Y);
      if (!localOnly)
        TSPlayer.All.SendTileSquareCentered(tileLocation.X, tileLocation.Y, 1);
    }

    public void RemoveTile(DPoint tileLocation, bool squareFrames = true, bool localOnly = false) {
      ITile tile = TerrariaUtils.Tiles[tileLocation];

      tile.wall = 0;
      tile.liquid = 0;

      this.RemoveBlock(tileLocation, squareFrames, localOnly);
    }

    /// <remarks>
    ///   <p>
    ///     A object is considered any tile type the player is not blocked from passing through plus 
    ///     Active Stone, Boulders, Wood Platforms and Dart Traps. However, this method will still measure
    ///     any other block type too.
    ///   </p>
    ///   <p>
    ///     The origin tile of most objects is their most top left tile. Exceptions are trees and glowing mushrooms where 
    ///     the origin tile is their middle stump. Vines have their origin tile at the root on top. Doors have their origin
    ///     tile at their top bracing.
    ///   </p>
    /// </remarks>
    public ObjectMeasureData MeasureObject(DPoint anyTileLocation) {
      ITile tile = TerrariaUtils.Tiles[anyTileLocation];
      if (!tile.active())
        throw new ArgumentException($"The tile at location {anyTileLocation} can not be measured because its not active");

      TileObjectData objectData = TileObjectData.GetTileData(tile);
      DPoint objectSize = new DPoint(1, 1);
      DPoint textureTileSize = new DPoint(TerrariaUtils.DefaultTextureTileSize, TerrariaUtils.DefaultTextureTileSize);
      if (objectData != null) {
        objectSize = new DPoint(objectData.Width, objectData.Height);
        textureTileSize = new DPoint(objectData.CoordinateWidth + objectData.CoordinatePadding, objectData.CoordinateHeights[0] + objectData.CoordinatePadding);
      }
      DPoint textureFrameLocation = DPoint.Empty;

      DPoint originTileLocation;
      switch (tile.type) {
        case TileID.Cactus: {
          // Removed dynamic measuring support for Cactus due to Terraria bugs...
          originTileLocation = anyTileLocation;
          textureFrameLocation = new DPoint(tile.frameX / textureTileSize.X, tile.frameY / textureTileSize.Y);
          break;
        }
        case TileID.Trees: 
        case TileID.MushroomTrees: {
          // Consider the origin tile of Trees, Giant Glowing Mushrooms and Cacti their most bottom tile (stump) instead
          // of the most top left tile.
          DPoint anyTrunkTileLocation = anyTileLocation;
          if (tile.type == TileID.Trees) {
            if (this.IsLeftTreeBranch(tile))
              anyTrunkTileLocation = anyTileLocation.OffsetEx(1, 0);
            else if (this.IsRightTreeBranch(tile))
              anyTrunkTileLocation = anyTileLocation.OffsetEx(-1, 0);
          } else if (tile.type == TileID.Cactus) {
            if (this.IsLeftCactusBranch(tile))
              anyTrunkTileLocation = anyTileLocation.OffsetEx(1, 0);
            else if (this.IsRightCactusBranch(tile))
              anyTrunkTileLocation = anyTileLocation.OffsetEx(-1, 0);
          }

          // Go all the way down to the tree's stump.
          DPoint currentTileLocation = anyTrunkTileLocation;
          while (true) {
            ITile currentTile = TerrariaUtils.Tiles[currentTileLocation.OffsetEx(0, 1)];

            if (currentTile.active() && currentTile.type == tile.type)
              currentTileLocation.Y++;
            else 
              break;
          }
          DPoint treeStumpLocation = currentTileLocation;
          objectSize = new DPoint(3, (treeStumpLocation.Y - anyTrunkTileLocation.Y) + 1);

          // Now measure the tree's size by going it up.
          currentTileLocation = anyTrunkTileLocation;
          while (true) {
            ITile currentTile = TerrariaUtils.Tiles[currentTileLocation.OffsetEx(0, -1)];

            if (currentTile.active() && currentTile.type == tile.type) {
              currentTileLocation.Y--;
              objectSize.Y++;
            } else {
              break;
            }
          }

          originTileLocation = treeStumpLocation;
          textureFrameLocation = new DPoint(tile.frameX / textureTileSize.X, tile.frameY / textureTileSize.Y);
          break;
        }
        case TileID.Vines:
        case TileID.JungleVines:
        case TileID.HallowedVines:
        case TileID.CrimsonVines: {
          DPoint currentTileLocation = anyTileLocation;

          while (true) {
            ITile currentTile = TerrariaUtils.Tiles[currentTileLocation.OffsetEx(0, -1)];

            if (currentTile.type == tile.type) {
              currentTileLocation.Y--;
              objectSize.Y++;
            } else {
              break;
            }
          }
          originTileLocation = currentTileLocation;
          objectSize = new DPoint(1, (anyTileLocation.Y - originTileLocation.Y) + 1);

          // Now measure the vines's size by going it down.
          currentTileLocation = anyTileLocation;
          while (true) {
            ITile currentTile = TerrariaUtils.Tiles[currentTileLocation.OffsetEx(0, 1)];

            if (currentTile.type == tile.type) {
              currentTileLocation.Y++;
              objectSize.Y++;
            } else {
              break;
            }
          }

          textureFrameLocation = new DPoint(tile.frameX / textureTileSize.X, tile.frameY / textureTileSize.Y);
          break;
        }
        case TileID.OpenDoor: 
        case TileID.TrapdoorOpen: {
          int textureTileX = tile.frameX / textureTileSize.X;

          if (this.GetDoorDirection(anyTileLocation) == Direction.Right ||
            this.GetDoorDirection(anyTileLocation) == Direction.Up) {
            originTileLocation = anyTileLocation.OffsetEx(-textureTileX, -(tile.frameY / textureTileSize.Y));
          } else {
            originTileLocation = anyTileLocation.OffsetEx(
              -(textureTileX - objectSize.X) + 1, -(tile.frameY / textureTileSize.Y)
            );
            textureFrameLocation = new DPoint(1, 0);
          }

          break;
        }
        default: {
          if (objectSize.X == 1 && objectSize.Y == 1) {
            originTileLocation = anyTileLocation;
            textureFrameLocation = new DPoint(tile.frameX / textureTileSize.X, tile.frameY / textureTileSize.Y);
          } else {
            int textureTileX = tile.frameX / textureTileSize.X;
            int textureTileY = tile.frameY / textureTileSize.Y;
            int textureFrameX = textureTileX / objectSize.X;
            int textureFrameY = textureTileY / objectSize.Y;

            originTileLocation = anyTileLocation.OffsetEx(
              -(textureTileX - (textureFrameX * objectSize.X)),
              -(textureTileY - (textureFrameY * objectSize.Y))
            );

            textureFrameLocation = new DPoint(textureFrameX, textureFrameY);
          }

          break;
        }
      }

      return new ObjectMeasureData(
        tile.type, originTileLocation, objectSize, textureTileSize, textureFrameLocation
      );
    }

    private bool IsLeftTreeBranch(ITile tile) {
      if (tile.type != TileID.Trees)
        return false;

      int frameX = tile.frameX;
      int frameY = tile.frameY;

      if (frameX == 44) {
        // Sub side or Green Branch
        return ((frameY >= 132 && frameY <= 176) || frameY >= 198);
      } else if (frameX == 66) {
        // Branch
        return (frameY >= 0 && frameY <= 44);
      } else {
        return false;
      }
    }

    private bool IsRightTreeBranch(ITile tile) {
      if (tile.type != TileID.Trees)
        return false;

      int frameX = tile.frameX;
      int frameY = tile.frameY;

      if (frameX == 22) {
        // Sub side
        return (frameY >= 132 && frameY <= 176);
      } else if (frameX == 66) {
        // Green Branch
        return (frameY >= 198);
      } else if (frameX == 88) {
        // Branch
        return (frameY >= 66 && frameY <= 110);
      } else {
        return false;
      }
    }

    private bool IsLeftCactusBranch(ITile tile) {
      if (tile.type != TileID.Cactus)
        return false;

      int frameX = tile.frameX;
      int frameY = tile.frameY;

      return (frameX == 54 || (frameX == 108 && frameY == 36));
    }

    private bool IsRightCactusBranch(ITile tile) {
      if (tile.type != TileID.Cactus)
        return false;

      int frameX = tile.frameX;
      int frameY = tile.frameY;

      return (frameX == 36 || (frameX == 108 && frameY == 16));
    }

    private static readonly Lazy<HashSet<int>> multistateObjectTypes = new Lazy<HashSet<int>>(() => {
      return new HashSet<int> {
        TileID.Torches,
        TileID.Candles,
        TileID.Chandeliers,
        TileID.HangingLanterns,
        TileID.Lampposts,
        TileID.Lamps,
        TileID.ChineseLanterns,
        TileID.Candelabras,
        TileID.DiscoBall,
        TileID.Lever,
        TileID.Switches,
        TileID.MusicBoxes,
        TileID.Timers,
        TileID.HolidayLights
      };
    });
    /// <remarks>
    ///   Objects whoose state can be changed by circuits.
    ///   Does not include doors or active stone.
    /// </remarks>
    public bool IsMultistateObject(int blockType) {
      return TerrariaTiles.multistateObjectTypes.Value.Contains(blockType);
    }

    public bool ObjectHasActiveState(ObjectMeasureData measureData) {
      ITile tile = TerrariaUtils.Tiles[measureData.OriginTileLocation];

      switch (measureData.BlockType) {
        case TileID.Switches:
          return (tile.frameY == 0);
        case TileID.Timers:
        case TileID.WaterFountain:
          return (tile.frameY != 0);
        case TileID.MusicBoxes:
          return (tile.frameX != 0);
        case TileID.Torches:
          return (tile.frameX < 66);
        case TileID.HolidayLights:
          return (tile.frameX < 54);
        default:
          return (tile.frameX == 0);
      }
    }

    public void SetObjectState(
      ObjectMeasureData measureData, bool activeState, bool sendTileSquare = true
    ) {
      #if DEBUG
      if (this.ObjectHasActiveState(measureData) == activeState) {
        throw new ArgumentException(
          $"The object \"{TerrariaUtils.Tiles.GetBlockTypeName(measureData.BlockType, 0)}\" does already have the state \"{activeState}\"."
        );
      }
      #endif

      int originX = measureData.OriginTileLocation.X;
      int originY = measureData.OriginTileLocation.Y;
      int objectWidth = measureData.Size.X;
      int objectHeight = measureData.Size.Y;
      int newFrameXOffset = 0;
      int newFrameYOffset = 0;
      if (measureData.BlockType == TileID.Torches || measureData.BlockType == TileID.HolidayLights)
        newFrameXOffset = measureData.TextureTileSize.X * 2;

      if (
        measureData.BlockType != TileID.Switches && 
        measureData.BlockType != TileID.Timers &&
        measureData.BlockType != TileID.WaterFountain
      ) {
        int frameXOffset = (objectWidth * measureData.TextureTileSize.X) + newFrameXOffset;
        if (measureData.BlockType == TileID.MusicBoxes)
          frameXOffset = -frameXOffset;

        if (activeState)
          newFrameXOffset = (short)-frameXOffset;
        else
          newFrameXOffset = (short)frameXOffset;

        if (measureData.BlockType == TileID.BubbleMachine && !activeState)
          newFrameYOffset = -TerrariaUtils.Tiles[measureData.OriginTileLocation].frameY;
      } else {
        int frameYOffset = (objectHeight * measureData.TextureTileSize.Y);
        if (measureData.BlockType == TileID.WaterFountain && !activeState) {
          newFrameYOffset = -TerrariaUtils.Tiles[measureData.OriginTileLocation].frameY;
        } else {
          if (
            measureData.BlockType == TileID.Timers ||
            measureData.BlockType == TileID.WaterFountain
          )
            activeState = !activeState;

          if (activeState)
            newFrameYOffset = (short)-frameYOffset;
          else
            newFrameYOffset = (short)frameYOffset;
        }
      }
        
      for (int tx = 0; tx < objectWidth; tx++) {
        for (int ty = 0; ty < objectHeight; ty++) {
          int absoluteX = originX + tx;
          int absoluteY = originY + ty;

          TerrariaUtils.Tiles[absoluteX, absoluteY].frameX += (short)newFrameXOffset;
          TerrariaUtils.Tiles[absoluteX, absoluteY].frameY += (short)newFrameYOffset;
        }
      }
            
      if (sendTileSquare)
        TSPlayer.All.SendTileSquareEx(originX, originY, Math.Max(objectWidth, objectHeight));
    }

    public bool IsObjectWired(DPoint originTileLocation, DPoint size, out DPoint firstWirePosition) {
      for (int tx = 0; tx < size.X; tx++) {
        for (int ty = 0; ty < size.Y; ty++) {
          int ax = originTileLocation.X + tx;
          int ay = originTileLocation.Y + ty;

          ITile tile = TerrariaUtils.Tiles[ax, ay];
          if (tile.HasWire()) {
            firstWirePosition = new DPoint(ax, ay);
            return true;
          }
        }
      }

      firstWirePosition = DPoint.Empty;
      return false;
    }

    public bool IsObjectWired(ObjectMeasureData measureData, WireColor wireColor, out DPoint firstWireLocation) {
      foreach (DPoint tileLocation in this.EnumerateObjectTileLocations(measureData)) {
        ITile tile = TerrariaUtils.Tiles[tileLocation];
        if (tile.HasWire(wireColor)) {
          firstWireLocation = tileLocation;
          return true;
        }
      }

      firstWireLocation = DPoint.Empty;
      return false;
    }

    public bool IsObjectWired(DPoint originTileLocation, DPoint size, WireColor wireColor = WireColor.None) {
      DPoint dummy;
      return this.IsObjectWired(originTileLocation, size, out dummy);
    }

    public bool IsObjectWired(ObjectMeasureData measureData, WireColor wireColor = WireColor.None) {
      DPoint dummy;
      return this.IsObjectWired(measureData, wireColor, out dummy);
    }

    public TrapStyle GetTrapStyle(int objectStyle) {
      return (TrapStyle)objectStyle;
    }

    public int GetItemTypeFromTrapStyle(TrapStyle trapStyle) {
      switch (trapStyle) {
        case TrapStyle.DartTrap:
          return ItemID.DartTrap;
        case TrapStyle.SuperDartTrap:
          return ItemID.SuperDartTrap;
        case TrapStyle.FlameTrap:
          return ItemID.FlameTrap;
        case TrapStyle.SpikyBallTrap:
          return ItemID.SpikyBallTrap;
        case TrapStyle.SpearTrap:
          return ItemID.SpearTrap;
        default:
          return ItemID.None;
      }
    }

    public StatueStyle GetStatueStyle(int objectStyle) {
      return (StatueStyle)(objectStyle + 1);
    }

    public StatueStyle GetStatueStyle(ITile tile) {
      return this.GetStatueStyle(tile.frameX / (TerrariaUtils.DefaultTextureTileSize * 2));
    }

    private static readonly Lazy<Dictionary<StatueStyle, int>> statueStyleItemTypeLookup = new Lazy<Dictionary<StatueStyle, int>>(() => {
      return new Dictionary<StatueStyle, int>{
        [StatueStyle.Armor] = ItemID.ArmorStatue,
        [StatueStyle.Angel] = ItemID.AngelStatue,
        [StatueStyle.Star] = ItemID.StarStatue,
        [StatueStyle.Sword] = ItemID.SwordStatue,
        [StatueStyle.Slime] = ItemID.SlimeStatue,
        [StatueStyle.Goblin] = ItemID.GoblinStatue,
        [StatueStyle.Shield] = ItemID.ShieldStatue,
        [StatueStyle.Bat] = ItemID.BatStatue,
        [StatueStyle.Fish] = ItemID.FishStatue,
        [StatueStyle.Bunny] = ItemID.BunnyStatue,
        [StatueStyle.Skeleton] = ItemID.SkeletonStatue,
        [StatueStyle.Reaper] = ItemID.ReaperStatue,
        [StatueStyle.Woman] = ItemID.WomanStatue,
        [StatueStyle.Imp] = ItemID.ImpStatue,
        [StatueStyle.Gargoyle] = ItemID.GargoyleStatue,
        [StatueStyle.Gloom] = ItemID.GloomStatue,
        [StatueStyle.Hornet] = ItemID.HornetStatue,
        [StatueStyle.Bomb] = ItemID.BombStatue,
        [StatueStyle.Crab] = ItemID.CrabStatue,
        [StatueStyle.Hammer] = ItemID.HammerStatue,
        [StatueStyle.Potion] = ItemID.PotionStatue,
        [StatueStyle.Spear] = ItemID.SpearStatue,
        [StatueStyle.Cross] = ItemID.CrossStatue,
        [StatueStyle.Jellyfish] = ItemID.JellyfishStatue,
        [StatueStyle.Bow] = ItemID.BowStatue,
        [StatueStyle.Boomerang] = ItemID.BoomerangStatue,
        [StatueStyle.Boot] = ItemID.BootStatue,
        [StatueStyle.Chest] = ItemID.ChestStatue,
        [StatueStyle.Bird] = ItemID.BirdStatue,
        [StatueStyle.Axe] = ItemID.AxeStatue,
        [StatueStyle.Corrupt] = ItemID.CorruptStatue,
        [StatueStyle.Tree] = ItemID.TreeStatue,
        [StatueStyle.Anvil] = ItemID.AnvilStatue,
        [StatueStyle.Pickaxe] = ItemID.PickaxeStatue,
        [StatueStyle.Mushroom] = ItemID.MushroomStatue,
        [StatueStyle.Eyeball] = ItemID.EyeballStatue,
        [StatueStyle.Pillar] = ItemID.PillarStatue,
        [StatueStyle.Heart] = ItemID.HeartStatue,
        [StatueStyle.Pot] = ItemID.PotStatue,
        [StatueStyle.Sunflower] = ItemID.SunflowerStatue,
        [StatueStyle.King] = ItemID.KingStatue,
        [StatueStyle.Queen] = ItemID.QueenStatue,
        [StatueStyle.Piranha] = ItemID.PiranhaStatue,
        [StatueStyle.Lihzahrd] = ItemID.LihzahrdStatue,
        [StatueStyle.LihzahrdGuardian] = ItemID.LihzahrdGuardianStatue,
        [StatueStyle.LihzahrdWatcher] = ItemID.LihzahrdWatcherStatue,
        [StatueStyle.BlueDungeonVase] = ItemID.BlueDungeonVase,
        [StatueStyle.GreenDungeonVase] = ItemID.GreenDungeonVase,
        [StatueStyle.PinkDungeonVase] = ItemID.PinkDungeonVase,
        [StatueStyle.ObsidianVase] = ItemID.ObsidianVase,
        [StatueStyle.Shark] = ItemID.SharkStatue,
        // 1.3.1
        [StatueStyle.Squirrel] = ItemID.SquirrelStatue,
        [StatueStyle.Butterfly] = ItemID.ButterflyStatue,
        [StatueStyle.Worm] = ItemID.WormStatue,
        [StatueStyle.Firefly] = ItemID.FireflyStatue,
        [StatueStyle.Scorpion] = ItemID.ScorpionStatue,
        [StatueStyle.Snail] = ItemID.SnailStatue,
        [StatueStyle.Grasshopper] = ItemID.GrasshopperStatue,
        [StatueStyle.Mouse] = ItemID.MouseStatue,
        [StatueStyle.Duck] = ItemID.DuckStatue,
        [StatueStyle.Penguin] = ItemID.PenguinStatue,
        [StatueStyle.Frog] = ItemID.FrogStatue,
        [StatueStyle.Buggy] = ItemID.BuggyStatue,
        [StatueStyle.WallCreeper] = ItemID.WallCreeperStatue,
        [StatueStyle.Unicorn] = ItemID.UnicornStatue,
        [StatueStyle.Drippler] = ItemID.DripplerStatue,
        [StatueStyle.Wraith] = ItemID.WraithStatue,
        [StatueStyle.BoneSkeleton] = ItemID.BoneSkeletonStatue,
        [StatueStyle.UndeadViking] = ItemID.UndeadVikingStatue,
        [StatueStyle.Medusa] = ItemID.MedusaStatue,
        [StatueStyle.Harpy] = ItemID.HarpyStatue,
        [StatueStyle.Pigron] = ItemID.PigronStatue,
        [StatueStyle.Hoplite] = ItemID.HopliteStatue,
        [StatueStyle.GraniteGolem] = ItemID.GraniteGolemStatue,
        [StatueStyle.ZombieArm] = ItemID.ZombieArmStatue,
        [StatueStyle.BloodZombie] = ItemID.BloodZombieStatue,
        // 1.4
        [StatueStyle.Bast] = ItemID.CatBast,
        [StatueStyle.Dragonfly] = ItemID.DragonflyStatue,
        [StatueStyle.Boulder] = ItemID.BoulderStatue,
        [StatueStyle.Seagull] = ItemID.SeagullStatue,
        [StatueStyle.Owl] = ItemID.OwlStatue,
        [StatueStyle.Turtle] = ItemID.TurtleStatue,
      };
    });
    public int GetItemTypeFromStatueStyle(StatueStyle statueStyle) {
      int itemType;
      if (!statueStyleItemTypeLookup.Value.TryGetValue(statueStyle, out itemType))
        throw new ArgumentException("StatueStyle");
      
      return itemType;
    }

    public PressurePlateKind GetPressurePlateKind(int objectStyle) {
      switch (objectStyle) {
        case 2:
        case 3:
        case 4:
        case 6:
        case 7:
          return PressurePlateKind.TriggeredByPlayers;
        case 0:
        case 1:
          return PressurePlateKind.TriggeredByPlayersNpcsEnemies;
        case 5:
          return PressurePlateKind.TriggeredByNpcsEnemies;
        default:
          return PressurePlateKind.Unknown;
      }
    }

    private static readonly Lazy<Tuple<ChestStyle, bool>[]> objectIdChestStyleLockedLookup = new Lazy<Tuple<ChestStyle, bool>[]>(() => {
      return new[] {
        new Tuple<ChestStyle, bool>(ChestStyle.WoodenChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.GoldChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.GoldChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.ShadowChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.ShadowChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.Barrel, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.TrashCan, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.EbonwoodChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.RichMahoganyChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.PearlwoodChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.IvyChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.IceChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.LivingWoodChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.SkywareChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.ShadewoodChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.WebCoveredChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.LihzahrdChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.WaterChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.JungleChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.CorruptionChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.CrimsonChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.HallowedChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.FrozenChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.JungleChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.CorruptionChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.CrimsonChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.HallowedChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.FrozenChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.CrimsonChest, true), 
        new Tuple<ChestStyle, bool>(ChestStyle.FrozenChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.DynastyChest, false), 
        new Tuple<ChestStyle, bool>(ChestStyle.HoneyChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.SteampunkChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.PalmWoodChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.MushroomChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.BorealWoodChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.SlimeChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.GreenDungeonChest, true),
        new Tuple<ChestStyle, bool>(ChestStyle.GreenDungeonChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.PinkDungeonChest, true),
        new Tuple<ChestStyle, bool>(ChestStyle.PinkDungeonChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.BlueDungeonChest, true),
        new Tuple<ChestStyle, bool>(ChestStyle.BlueDungeonChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.BoneChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.CactusChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.FleshChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.ObsidianChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.PumpkinChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.SpookyChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.GlassChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.MartianChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.MeteoriteChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.GraniteChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.MarbleChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.CrystalChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.GoldenChest, false),
      };
    });
    private static readonly Lazy<Tuple<ChestStyle, bool>[]> objectIdChest2StyleLockedLookup = new Lazy<Tuple<ChestStyle, bool>[]>(() => {
      return new[] {
        new Tuple<ChestStyle, bool>(ChestStyle.CrystalChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.GoldenChest, false),
        //1.4
        new Tuple<ChestStyle, bool>(ChestStyle.SpiderChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.LesionChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.DeadMansChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.SolarChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.VortexChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.NebulaChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.StardustChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.GolfChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.SandstoneChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.BambooChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.DesertChest, false),
        new Tuple<ChestStyle, bool>(ChestStyle.DesertChest, true),
      };
    });
    public ChestStyle GetChestStyle(ushort tileId, int objectStyle, out bool isLocked) {
      var lookupTable = objectIdChestStyleLockedLookup;
      if (tileId == TileID.Containers2)
        lookupTable =  objectIdChest2StyleLockedLookup;

      if (objectStyle < 0 || objectStyle >= lookupTable.Value.Length)
        throw new ArgumentOutOfRangeException(nameof(objectStyle));

      Tuple<ChestStyle, bool> lookupItem = lookupTable.Value[objectStyle];
      isLocked = lookupItem.Item2;
      return lookupItem.Item1;
    }

    public ChestStyle GetChestStyle(ITile tile, out bool isLocked) {
      return this.GetChestStyle(tile.type, (tile.frameX / (TerrariaUtils.DefaultTextureTileSize * 2)), out isLocked);
    }

    private static readonly Lazy<Dictionary<ChestStyle, int>> chestStyleKeyItemTypeLookup = new Lazy<Dictionary<ChestStyle, int>>(() => {
      return new Dictionary<ChestStyle, int>{
        [ChestStyle.GoldChest] = ItemID.GoldenKey,
        [ChestStyle.ShadowChest] = ItemID.ShadowKey,
        [ChestStyle.CorruptionChest] = ItemID.CorruptionKey,
        [ChestStyle.CrimsonChest] = ItemID.CrimsonKey,
        [ChestStyle.HallowedChest] = ItemID.HallowedKey,
        [ChestStyle.FrozenChest] = ItemID.FrozenKey,
        [ChestStyle.JungleChest] = ItemID.JungleKey,
        [ChestStyle.DesertChest] = ItemID.DungeonDesertKey,
      };
    });
    public int KeyItemTypeFromChestStyle(ChestStyle chestStyle) {
      int itemType;
      if (!chestStyleKeyItemTypeLookup.Value.TryGetValue(chestStyle, out itemType))
        throw new ArgumentException("ChestStyle");
      
      return itemType;
    }

    public bool IsChestLocked(ITile tile) {
      bool isLocked;
      this.GetChestStyle(tile.type, (tile.frameX / (TerrariaUtils.DefaultTextureTileSize * 2)), out isLocked);
      return isLocked;
    }

    private static readonly Lazy<Dictionary<ChestStyle, int>> chestStyleItemTypeLookup = new Lazy<Dictionary<ChestStyle, int>>(() => {
      return new Dictionary<ChestStyle, int>{
        [ChestStyle.WoodenChest] = ItemID.Chest,
        [ChestStyle.GoldChest] = ItemID.GoldChest,
        [ChestStyle.ShadowChest] = ItemID.ShadowChest,
        [ChestStyle.Barrel] = ItemID.Barrel,
        [ChestStyle.EbonwoodChest] = ItemID.EbonwoodChest,
        [ChestStyle.RichMahoganyChest] = ItemID.RichMahoganyChest,
        [ChestStyle.PearlwoodChest] = ItemID.PearlwoodChest,
        [ChestStyle.IvyChest] = ItemID.IvyChest,
        [ChestStyle.IceChest] = ItemID.IceChest,
        [ChestStyle.LivingWoodChest] = ItemID.LivingWoodChest,
        [ChestStyle.SkywareChest] = ItemID.SkywareChest,
        [ChestStyle.ShadewoodChest] = ItemID.ShadewoodChest,
        [ChestStyle.WebCoveredChest] = ItemID.WebCoveredChest,
        [ChestStyle.LihzahrdChest] = ItemID.LihzahrdChest,
        [ChestStyle.WaterChest] = ItemID.WaterChest,
        [ChestStyle.JungleChest] = ItemID.JungleChest,
        [ChestStyle.CorruptionChest] = ItemID.CorruptionChest,
        [ChestStyle.CrimsonChest] = ItemID.CrimsonChest,
        [ChestStyle.HallowedChest] = ItemID.HallowedChest,
        [ChestStyle.FrozenChest] = ItemID.FrozenChest,
        [ChestStyle.DynastyChest] = ItemID.DynastyChest,
        [ChestStyle.HoneyChest] = ItemID.HoneyChest,
        [ChestStyle.SteampunkChest] = ItemID.SteampunkChest,
        [ChestStyle.PalmWoodChest] = ItemID.PalmWoodChest,
        [ChestStyle.MushroomChest] = ItemID.MushroomChest,
        [ChestStyle.BorealWoodChest] = ItemID.BorealWoodChest,
        [ChestStyle.SlimeChest] = ItemID.SlimeChest,
        [ChestStyle.GreenDungeonChest] = ItemID.GreenDungeonChest,
        [ChestStyle.PinkDungeonChest] = ItemID.PinkDungeonChest,
        [ChestStyle.BlueDungeonChest] = ItemID.BlueDungeonChest,
        [ChestStyle.BoneChest] = ItemID.BoneChest,
        [ChestStyle.CactusChest] = ItemID.CactusChest,
        [ChestStyle.FleshChest] = ItemID.FleshChest,
        [ChestStyle.ObsidianChest] = ItemID.ObsidianChest,
        [ChestStyle.PumpkinChest] = ItemID.PumpkinChest,
        [ChestStyle.SpookyChest] = ItemID.SpookyChest,
        [ChestStyle.GlassChest] = ItemID.GlassChest,
        [ChestStyle.MartianChest] = ItemID.MartianChest,
        [ChestStyle.MeteoriteChest] = ItemID.MeteoriteChest,
        [ChestStyle.GraniteChest] = ItemID.GraniteChest,
        [ChestStyle.MarbleChest] = ItemID.MarbleChest,
        [ChestStyle.CrystalChest] = ItemID.CrystalChest,
        [ChestStyle.GoldenChest] = ItemID.GoldenChest,
        // 1.4
        [ChestStyle.SpiderChest] = ItemID.SpiderChest,
        [ChestStyle.LesionChest] = ItemID.LesionChest,
        [ChestStyle.DeadMansChest] = ItemID.DeadMansChest,
        [ChestStyle.SolarChest] = ItemID.SolarChest,
        [ChestStyle.VortexChest] = ItemID.VortexChest,
        [ChestStyle.NebulaChest] = ItemID.NebulaChest,
        [ChestStyle.StardustChest] = ItemID.StardustChest,
        [ChestStyle.GolfChest] = ItemID.GolfChest,
        [ChestStyle.SandstoneChest] = ItemID.DesertChest,
        [ChestStyle.BambooChest] = ItemID.BambooChest,
        [ChestStyle.DesertChest] = ItemID.DungeonDesertChest,
      };
    });
    public int GetItemTypeFromChestStyle(ChestStyle chestStyle) {
      int itemType;
      if (!chestStyleItemTypeLookup.Value.TryGetValue(chestStyle, out itemType))
        throw new ArgumentException("ChestStyle");
      
      return itemType;
    }

    private static readonly Lazy<HashSet<ChestStyle>> lockableChestStyles = new Lazy<HashSet<ChestStyle>>(() => {
      return new HashSet<ChestStyle>{
        ChestStyle.GoldChest,
        ChestStyle.ShadowChest,
        ChestStyle.JungleChest,
        ChestStyle.CorruptionChest,
        ChestStyle.CrimsonChest,
        ChestStyle.HallowedChest,
        ChestStyle.FrozenChest,
        ChestStyle.BlueDungeonChest,
        ChestStyle.GreenDungeonChest,
        ChestStyle.PinkDungeonChest,
        ChestStyle.DesertChest,
      };
    });
    public void LockChest(DPoint anyChestTileLocation) {
      ITile chestTile = TerrariaUtils.Tiles[anyChestTileLocation];
      if (!chestTile.active() || (chestTile.type != TileID.Containers && chestTile.type != TileID.Containers2))
        throw new ArgumentException($"Tile is not a chest (location: {anyChestTileLocation}).", nameof(anyChestTileLocation));

      bool isLocked;
      ChestStyle chestStyle = this.GetChestStyle(chestTile, out isLocked);
      if (isLocked)
        throw new InvalidChestStyleException($"Chest is already locked (location: {anyChestTileLocation}).", chestStyle);

      if (!this.IsChestStyleLockable(chestStyle))
        throw new InvalidChestStyleException($"Chest has to be a lockable chest (location: {anyChestTileLocation}).", chestStyle);

      ObjectMeasureData measureData = this.MeasureObject(anyChestTileLocation);
      if (
        chestStyle == ChestStyle.JungleChest ||
        chestStyle == ChestStyle.CorruptionChest ||
        chestStyle == ChestStyle.CrimsonChest ||
        chestStyle == ChestStyle.HallowedChest ||
        chestStyle == ChestStyle.FrozenChest)
      {
        foreach (ITile tile in this.EnumerateObjectTiles(measureData))
          tile.frameX += 180;
      }
      else
      {
        foreach (ITile tile in this.EnumerateObjectTiles(measureData))
          tile.frameX += 36;
      }
      
      TSPlayer.All.SendTileSquareCentered(anyChestTileLocation, 4);
    }

    public bool IsChestStyleLockable(ChestStyle chestKind) {
      return lockableChestStyles.Value.Contains(chestKind);
    }
 
    public ChestKind GuessChestKind(DPoint anyChestTileLocation) {
      ITile chestTile = TerrariaUtils.Tiles[anyChestTileLocation];
      if (!chestTile.active())
        throw new ArgumentException("The tile on the given location is not active.");

      bool isLocked;
      ChestStyle chestStyle = this.GetChestStyle(chestTile, out isLocked);
      switch (chestStyle) {
        case ChestStyle.GoldChest:
          if (
            chestTile.wall >= WallID.BlueDungeonUnsafe && chestTile.wall <= WallID.PinkDungeonUnsafe ||
            chestTile.wall >= WallID.BlueDungeonSlabUnsafe && chestTile.wall <= WallID.GreenDungeonTileUnsafe
          )
            return ChestKind.DungeonChest;

          if (chestStyle == ChestStyle.GoldChest && chestTile.wall == WallID.SandstoneBrick){
            if (anyChestTileLocation.Y < Main.worldSurface - 250 || anyChestTileLocation.Y > Main.worldSurface + 50)
              return ChestKind.Unknown;
            else
              return ChestKind.PyramidChest;
          }
          
          return ChestKind.Unknown;

        case ChestStyle.WaterChest:
          if (chestTile.liquid < 255 || chestTile.lava())
            return ChestKind.Unknown;
          if (anyChestTileLocation.X > 250 && anyChestTileLocation.X < Main.maxTilesX - 250)
            return ChestKind.Unknown;
          if (anyChestTileLocation.Y < Main.worldSurface - 200 || anyChestTileLocation.Y > Main.worldSurface + 50)
            return ChestKind.Unknown;

          return ChestKind.OceanChest;

        case ChestStyle.FrozenChest:
        case ChestStyle.HallowedChest:
        case ChestStyle.JungleChest:
        case ChestStyle.CrimsonChest:
        case ChestStyle.CorruptionChest:
        case ChestStyle.DesertChest:
          if (isLocked) {
            if (
              chestTile.wall >= WallID.BlueDungeonUnsafe && chestTile.wall <= WallID.PinkDungeonUnsafe ||
              chestTile.wall >= WallID.BlueDungeonSlabUnsafe && chestTile.wall <= WallID.GreenDungeonTileUnsafe
            )
              return ChestKind.HardmodeDungeonChest;
          }
          
          return ChestKind.Unknown;

        case ChestStyle.ShadowChest:
          if (!isLocked)
            return ChestKind.Unknown;
          
          if (anyChestTileLocation.Y < Main.maxTilesY * (1.0 - 0.143))
            return ChestKind.Unknown;

          return ChestKind.HellShadowChest;

        //case ChestStyle.WoodenChest:
        //  if (chestTile.wall >= (int)WallType.DungeonBlueBrickWall && chestTile.wall <= (int)WallType.DungeonPinkBrickWall)
        //    return ChestKind.DungeonChest;

        //  return ChestKind.Unknown;

        case ChestStyle.SkywareChest:
          if (anyChestTileLocation.Y < Main.worldSurface)
            return ChestKind.SkyIslandChest;

          return ChestKind.Unknown;
        default:
          return ChestKind.Unknown;
      }
    }

    public Direction GetDoorDirection(DPoint anyDoorTileLocation) {
      ITile anyDoorTile = TerrariaUtils.Tiles[anyDoorTileLocation];
      if (!anyDoorTile.active())
        throw new ArgumentException("The tile is not active.");
      
      if (anyDoorTile.type == TileID.ClosedDoor
        || anyDoorTile.type == TileID.TrapdoorClosed)
        return Direction.Unknown;

      if (anyDoorTile.frameX < 36)
        return anyDoorTile.type == TileID.OpenDoor ? Direction.Right : Direction.Up;
      else
        return anyDoorTile.type == TileID.OpenDoor ? Direction.Left : Direction.Down;
    }

    public HerbStyle GetHerbStyle(int objectStyle) {
      return (HerbStyle)(objectStyle + 1);
    }

    public HerbStyle GetHerbStyle(ITile tile) {
      return this.GetHerbStyle(tile.frameX / TerrariaUtils.DefaultTextureTileSize);
    }

    public IEnumerable<DPoint> EnumerateObjectTileLocations(ObjectMeasureData measureData) {
      DPoint origin = measureData.OriginTileLocation;

      if (
        (measureData.BlockType == TileID.OpenDoor &&
        this.GetDoorDirection(measureData.OriginTileLocation) == Direction.Left) ||
        (measureData.BlockType == TileID.TrapdoorOpen &&
        this.GetDoorDirection(measureData.OriginTileLocation) == Direction.Down )
      ) {
        origin.Offset(-1, 0);
      }

      for (int x = origin.X; x < origin.X + measureData.Size.X; x++)
        for (int y = origin.Y; y < origin.Y + measureData.Size.Y; y++)
          yield return new DPoint(x, y);
    }

    public IEnumerable<ITile> EnumerateObjectTiles(ObjectMeasureData measureData) {
      foreach (DPoint tileLocation in this.EnumerateObjectTileLocations(measureData))
        yield return TerrariaUtils.Tiles[tileLocation];
    }

    public IEnumerable<ITile> EnumerateTilesRectangularAroundPoint(DPoint tileLocation, int rectWidth, int rectHeight) {
      int halfWidth = (rectWidth / 2);
      int halfHeight = (rectHeight / 2);
      for (int x = tileLocation.X - halfWidth; x <= tileLocation.X + halfWidth; x++) {
        for (int y = tileLocation.Y - halfHeight; y <= tileLocation.Y + halfHeight; y++) {
          yield return TerrariaUtils.Tiles[x, y];
        }
      }
    }

    public DPoint GetObjectSize(int blockType) {
      TileObjectData objectData = TileObjectData.GetTileData(blockType, 0);
      if (objectData != null)
        return new DPoint(objectData.Width, objectData.Height);
      else
        return new DPoint(1, 1);
    }

    public Direction GetObjectOrientation(ITile anyTile) {
      TileObjectData objectData = TileObjectData.GetTileData(anyTile);
      if (objectData == null)
        return Direction.Unknown;

      if (objectData.AnchorBottom.type != 0)
        return Direction.Up;
      if (objectData.AnchorLeft.type != 0)
        return Direction.Right;
      if (objectData.AnchorRight.type != 0)
        return Direction.Left;
      if (objectData.AnchorTop.type != 0)
        return Direction.Down;

      return Direction.Unknown;
   ; }

    /*public Direction GetObjectOrientation(Tile anyTile) {
      if (!anyTile.active())
        return Direction.Unknown;

      switch (anyTile.type) {
        case BlockType.Torch:
          if (anyTile.frameX == 44 || anyTile.frameX == 110)
            return Direction.Left;

          if (anyTile.frameX == 22 || anyTile.frameX == 88)
            return Direction.Right;

          return Direction.Up;
        case BlockType.Sign:
          if (anyTile.frameX < 36)
            return Direction.Up;

          if (anyTile.frameX == 36 || anyTile.frameX == 54)
            return Direction.Down;

          if (anyTile.frameX == 72 || anyTile.frameX == 90)
            return Direction.Right;

          return Direction.Left;
        case BlockType.CrystalShard:
          if (anyTile.frameY == 0)
            return Direction.Up;

          if (anyTile.frameY == 18)
            return Direction.Down;

          if (anyTile.frameY == 36)
            return Direction.Left;

          return Direction.Right;
        case BlockType.Switch:
          if (anyTile.frameX == 0)
            return Direction.Up;

          if (anyTile.frameX == 18)
            return Direction.Right;

          return Direction.Left;
        case BlockType.Undefined2:
          if (
            (anyTile.frameX >= 54 && anyTile.frameX <= 90) &&
            (anyTile.frameY == 36 || anyTile.frameY == 54 || anyTile.frameY == 90)
          ) {
            return Direction.Up;
          }
          if (anyTile.frameX >= 162 && anyTile.frameX <= 198 && anyTile.frameY == 90)
            return Direction.Up;

          return Direction.Down;
        case BlockType.Amber:
        case BlockType.Undefined8:
          if (anyTile.frameY >= 162)
            return Direction.Left;

          if (anyTile.frameY >= 108)
            return Direction.Right;

          if (anyTile.frameY >= 54)
            return Direction.Down;

          return Direction.Up;
        default:
          return Direction.Unknown;
      }
    }*/
  }
}
