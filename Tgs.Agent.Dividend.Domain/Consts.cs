using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain;

public static class Consts
{
    public static GameType[] SlotGroup =
    {
        GameType.Fishing,
        GameType.Arcade,
        GameType.Slot,
        GameType.TableGame
    };

    public static GameType[] SportsGroup =
    {
        GameType.Sportsbook,
        GameType.ESports
    };
}