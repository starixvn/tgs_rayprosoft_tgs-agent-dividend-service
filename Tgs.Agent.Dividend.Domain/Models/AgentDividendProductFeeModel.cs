using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendProductFeeModel
{
    public int AgentDividendId { get; set; }
    public Guid AgentId { get; set; }
    public GameType GameType { get; set; }
    public decimal Percentage { get; set; }
    public decimal WinLossAmount { get; set; }

    public GameGroupType GameGroupType => GameType switch
    {
        GameType.Arcade => GameGroupType.Slot,
        GameType.Fishing => GameGroupType.Slot,
        GameType.Slot => GameGroupType.Slot,
        GameType.TableGame => GameGroupType.Slot,
        GameType.Sportsbook => GameGroupType.Sportsbook,
        GameType.ESports => GameGroupType.Sportsbook,
        GameType.VirtualSports => GameGroupType.VirtualSports,
        GameType.LiveDealer => GameGroupType.LiveDealer,
        GameType.CardGame => GameGroupType.CardGame,
        GameType.Lottery => GameGroupType.Lottery,
        _ => throw new ArgumentOutOfRangeException()
    };
}