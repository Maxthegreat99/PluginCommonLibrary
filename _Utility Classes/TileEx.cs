using System;

namespace Terraria.Plugins.Common {
  public static class TileEx {
    public static bool HasWire(this ITile tile, WireColor color = WireColor.None) {
      if (color == WireColor.None)
        return tile.wire() || tile.wire2() || tile.wire3() || tile.wire4();
      if (color == WireColor.Red)
        return tile.wire();
      if (color == WireColor.Blue)
        return tile.wire2();
      if (color == WireColor.Green)
        return tile.wire3();
      if (color == WireColor.Yellow)
        return tile.wire4();

      return false;
    }
  }
}
