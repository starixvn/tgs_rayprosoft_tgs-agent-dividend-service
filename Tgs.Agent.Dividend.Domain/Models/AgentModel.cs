using Microsoft.EntityFrameworkCore;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentModel
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Username { get; set; } = null!;
    public Guid? ParentAgentId { get; set; }
    public LTree Path { get; set; }
}