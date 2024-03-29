using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Forest.Contracts.SymbolRegistrar
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class SymbolRegistrarContractTestModule : MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContractInitializationProvider, SymbolRegistrarContractInitializationProvider>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);

        }
        //
        // public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        // {
        //     var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
        //     var contractDllLocation = typeof(SymbolRegistrarContract).Assembly.Location;
        //     var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
        //     {
        //         {
        //             new SymbolRegistrarContractInitializationProvider().ContractCodeName,
        //             File.ReadAllBytes(contractDllLocation)
        //         }
        //     };
        //     contractCodeProvider.Codes = contractCodes;
        // }
    }
}