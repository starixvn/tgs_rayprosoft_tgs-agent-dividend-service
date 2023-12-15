using Microsoft.EntityFrameworkCore;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentParentModel
{
    public int Rank { get; set; }
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public LTree Path { get; set; }
}