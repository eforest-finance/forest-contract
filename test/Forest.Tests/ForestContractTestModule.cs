using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.Forest;
using AElf.Contracts.Whitelist;
using AElf.Contracts.NFT;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Forest
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class ForestContractTestModule : MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
            context.Services
                .AddSingleton<IContractDeploymentListProvider, MainChainDAppContractTestDeploymentListProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>() ??
                                       new ContractCodeProvider();
            var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
            {
                {
                    new ForestContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(ForestContract).Assembly.Location)
                },
                {
                    new WhitelistContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(WhitelistContract).Assembly.Location)
                },
                {
                    new NFTContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(NFTContract).Assembly.Location)
                }
            };
            contractCodeProvider.Codes = contractCodes;
        }
    }
}