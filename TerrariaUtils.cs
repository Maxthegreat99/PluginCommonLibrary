using System;
using System.Collections;
using System.Collections.Generic;
using DPoint = System.Drawing.Point;

namespace Terraria.Plugins.Common {
  public static partial class TerrariaUtils {
    public static int BlockType_Min = 0;
    public static int BlockType_Max = Terraria.ID.TileID.Count - 1;
    public static int WallType_Min = 0;
    public static int WallType_Max = Terraria.ID.WallID.Count - 1;
    public static int ItemType_Min = 0;
    public static int ItemType_Max = Terraria.ID.ItemID.Count - 1;
    public static int NpcType_Min = -65;
    public static int NpcType_Max = ID.NPCID.Count - 1;
    public const int DefaultTextureTileSize = 18;
    public const int TileSize = 16;

    public static TerrariaTiles Tiles { get; private set; }
    public static TerrariaItems Items { get; private set; }
    public static TerrariaNpcs Npcs { get; private set; }
    public static TerrariaProjectiles Projectiles { get; private set; }

    public static InvasionType InvasionType => (Common.InvasionType)Main.invasionType;


    static TerrariaUtils() {
      TerrariaUtils.Tiles = new TerrariaTiles();
      TerrariaUtils.Items = new TerrariaItems();
      TerrariaUtils.Npcs = new TerrariaNpcs();
      TerrariaUtils.Projectiles = new TerrariaProjectiles();
    }

    public static IEnumerable<int> EnumeratePlayerIndexesAroundPoint(DPoint location, float radius) {
      for (int i = 0; i < Main.player.Length; i++) {
        Player player = Main.player[i];
        if (player == null || !player.active)
          continue;

        if (Math.Sqrt(Math.Pow(player.position.X - location.X, 2) + Math.Pow(player.position.Y - location.Y, 2)) <= radius)
          yield return i;
      }
    }
  }
}
