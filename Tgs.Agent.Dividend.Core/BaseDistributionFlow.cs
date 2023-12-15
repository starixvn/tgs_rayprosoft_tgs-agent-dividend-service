//using Microsoft.Extensions.Logging;
//using Tgs.Agent.Dividend.Domain.Interfaces;
//using Tgs.Agent.Dividend.Domain.Models;

//namespace Tgs.Agent.Dividend.Core
//{
//    internal abstract class BaseDistributionFlow : IDistribution
//    {
//        private readonly ILogger<BaseDistributionFlow> _logger;

//        protected BaseDistributionFlow(ILogger<BaseDistributionFlow> logger)
//        {
//            _logger = logger;
//        }

//        public virtual Task<IDistribution> CalculateBonusAmountAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("CalculateBonus");
//            return null;
//        }

//        public virtual Task<IDistribution> DistributeBonusAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("DistributeBonus");
//            return null;
//        }

//        public virtual Task<IDistribution> SetConditionDataAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("SetConditionData");
//            return null;
//        }

//        public virtual IDistribution SetContract(Guid brandId)
//        {
//            _logger.LogInformation("SetContract");
//            return null;
//        }

//        public IAsyncEnumerable<CreateAgentDividendModel> AgentDividendGenerationAsync(int idNumber,
//            AgentDividendContractModel data,
//            IEnumerable<AgentPlayerAggregateModel> agentPlayerAggregates,
//            CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public IAsyncEnumerable<CreateAgentDividendModel> GetAllAgentDividend()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

