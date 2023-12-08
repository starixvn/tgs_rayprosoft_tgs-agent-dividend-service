namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentPlayerDepositModel
{
    public Guid AgentId { get; set; }
    public Guid BrandId { get; set; }
    public string PlayerCode { get; set; } = null!;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal DepositAmount { get; set; }
}