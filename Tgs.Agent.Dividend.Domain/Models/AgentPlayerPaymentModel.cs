namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentPlayerPaymentModel
{
    public Guid AgentId { get; set; }
    public Guid BrandId { get; set; }
    public string PlayerCode { get; set; } = null!;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal WithdrawAmount { get; set; }
}