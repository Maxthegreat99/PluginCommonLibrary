using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using DPoint = System.Drawing.Point;

using TShockAPI;

namespace Terraria.Plugins.Common {
  public static class TShockEx {
    public static bool MatchUserAccountNameByPlayerName(string playerName, out string exactName, TSPlayer messagesReceiver = null) {
      exactName = null;
      TShockAPI.DB.UserAccount tsUser = TShock.UserAccounts.GetUserAccountByName(playerName);
      if (tsUser == null) {
        TSPlayer player;
        if (!TShockEx.MatchPlayerByName(playerName, out player, messagesReceiver))
          return false;

        exactName = player.Account.Name;
      } else {
        exactName = tsUser.Name;
      }

      return true;
    }

    public static bool MatchUserIdByPlayerName(string playerName, out int userId, TSPlayer messagesReceiver = null) {
      userId = -1;
      TShockAPI.DB.UserAccount tsUser = TShock.UserAccounts.GetUserAccountByName(playerName);
      if (tsUser == null) {
        TSPlayer player;
        if (!TShockEx.MatchPlayerByName(playerName, out player, messagesReceiver))
          return false;

        userId = player.Account.ID;
      } else {
        userId = tsUser.ID;
      }

      return true;
    }

    public static bool MatchUserByPlayerName(string playerName, out TShockAPI.DB.UserAccount user, TSPlayer messagesReceiver = null) {
      user = null;
      TShockAPI.DB.UserAccount tsUser = TShock.UserAccounts.GetUserAccountByName(playerName);
      if (tsUser == null) {
        TSPlayer player;
        if (!TShockEx.MatchPlayerByName(playerName, out player, messagesReceiver))
          return false;

        user = TShock.UserAccounts.GetUserAccountByID(player.Account.ID);
      } else {
        user = tsUser;
      }

      return true;
    }

    public static bool MatchPlayerByName(
      string name, out TSPlayer matchedPlayer, TSPlayer messagesReceiver = null
    ) {
      matchedPlayer = null;
      List<TSPlayer> matchedPlayers = TSPlayer.FindByNameOrID(name);
      if (matchedPlayers.Count == 0) {
        if (messagesReceiver != null)
          messagesReceiver.SendErrorMessage($"Could not match any players for \"{name}\".");

        return false;
      } if (matchedPlayers.Count > 1) {
        if (messagesReceiver != null) {
          messagesReceiver.SendErrorMessage(
            "More than one player matched! Matches: " + string.Join(", ", matchedPlayers.Select(p => p.Name))
          );
        }
        return false;
      }

      matchedPlayer = matchedPlayers[0];
      return true;
    }

    public static TSPlayer GetPlayerByName(string name, StringComparison stringComparison = StringComparison.InvariantCulture) {
      foreach (TSPlayer tsPlayer in TShock.Players) {
        if (tsPlayer == null)
          continue;

        if (tsPlayer.Name.Equals(name, stringComparison))
          return tsPlayer;
      }

      return null;
    }

    public static TSPlayer GetPlayerByIp(string ip) {
      foreach (TSPlayer tsPlayer in TShock.Players) {
        if (tsPlayer == null)
          continue;

        if (tsPlayer.IP == ip)
          return tsPlayer;
      }

      return null;
    }

    public static TSPlayer GetPlayerByTPlayer(Player tPlayer) {
      foreach (TSPlayer tsPlayer in TShock.Players) {
        if (tsPlayer == null)
          continue;

        if (tsPlayer.TPlayer == tPlayer)
          return tsPlayer;
      }

      return null;
    }

    public static void SendTileSquareCentered(this TSPlayer player, DPoint location, byte size = 10) {
      player.SendTileSquareCentered(location.X, location.Y, size);
    }

    /// <summary>
    ///   Uses x, y as the top left origin and size as width, height.
    /// </summary>
    public static void SendTileSquareEx(this TSPlayer player, int x, int y, int size = 10) {
      player.SendData(PacketTypes.TileSendSquare, string.Empty, size, x, y);
    }

    /// <summary>
    ///   Uses x, y as the top left origin and size as width, height.
    /// </summary>
    public static void SendTileSquareEx(this TSPlayer player, DPoint location, int size = 10) {
      TShockEx.SendTileSquareEx(player, location.X, location.Y, size);
    }

    public static DPoint ToLocation(this TSPlayer player) {
      return new DPoint((int)player.X, (int)player.Y);
    }

    public static DPoint ToTileLocation(this TSPlayer player) {
      return new DPoint(player.TileX, player.TileY);
    }

    public static string ParamsToSingleString(this CommandArgs args, int fromIndex = 0, int paramsToTrimFromEnd = 0) {
      StringBuilder builder = new StringBuilder();
      for (int i = fromIndex; i < args.Parameters.Count - paramsToTrimFromEnd; i++) {
        if (i > fromIndex)
          builder.Append(' ');

        builder.Append(args.Parameters[i]);
      }

      return builder.ToString();
    }

    public static bool ContainsParameter(this CommandArgs args, string parameter, StringComparison comparison) {
      foreach (string param in args.Parameters) {
        if (param.Equals(parameter, comparison))
          return true;
      }

      return false;
    }
  }
}
