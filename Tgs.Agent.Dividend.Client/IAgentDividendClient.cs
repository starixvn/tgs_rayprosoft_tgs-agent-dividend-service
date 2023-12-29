namespace Tgs.Agent.Dividend.Client;

public interface IAgentDividendClient
{
    Task GenerateAgentDividend();
    Task GenerateAgentDividend(Guid brandId);
}