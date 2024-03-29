using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using ProxyAccountContractContainer = AElf.Contracts.ProxyAccountContract.ProxyAccountContractContainer;

namespace Forest.Contracts.SymbolRegistrar
{
    public class SymbolRegistrarContractTestBase : DAppContractTestBase<SymbolRegistrarContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);
        protected const int MinersCount = 1;

        internal Address ProxyAccountContractAddress { get; set; }
        internal Address SymbolRegistrarContractAddress { get; set; }
        internal Account Admin => Accounts[0];
        internal Account User1 => Accounts[1];
        internal Account User2 => Accounts[2];
        internal Account User3 => Accounts[3];
        internal Account ReceivingAccount => Accounts[10];
        internal TokenContractContainer.TokenContractStub AdminTokenContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub User1TokenContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub User2TokenContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub User3TokenContractStub { get; set; }
        
        internal AssociationContractImplContainer.AssociationContractImplStub AdminAssociationContractStub { get; set; }

        internal AssociationContractImplContainer.AssociationContractImplStub User1AssociationContractStub { get; set; }
        internal AssociationContractImplContainer.AssociationContractImplStub User2AssociationContractStub { get; set; }
        internal AssociationContractImplContainer.AssociationContractImplStub User3AssociationContractStub { get; set; }


        internal SymbolRegistrarContractContainer.SymbolRegistrarContractStub AdminSymbolRegistrarContractStub { get; set; }
        internal SymbolRegistrarContractContainer.SymbolRegistrarContractStub User1SymbolRegistrarContractStub { get; set; }
        internal SymbolRegistrarContractContainer.SymbolRegistrarContractStub User2SymbolRegistrarContractStub { get; set; }
        internal SymbolRegistrarContractContainer.SymbolRegistrarContractStub User3SymbolRegistrarContractStub { get; set; }
         
        internal ProxyAccountContractContainer.ProxyAccountContractStub AdminProxyAccountContractStubContractStub { get; set; }
        
        protected readonly IBlockTimeProvider BlockTimeProvider;
        
        protected SymbolRegistrarContractTestBase()
        {
            DeploySaleContract(Admin);

            AdminTokenContractStub = GetTokenContractStub(Admin.KeyPair);
            User1TokenContractStub = GetTokenContractStub(User1.KeyPair);
            User2TokenContractStub = GetTokenContractStub(User2.KeyPair);
            User3TokenContractStub = GetTokenContractStub(User3.KeyPair);

            AdminSymbolRegistrarContractStub = GetSymbolRetistrarStub(Admin.KeyPair);
            User1SymbolRegistrarContractStub = GetSymbolRetistrarStub(User1.KeyPair);
            User2SymbolRegistrarContractStub = GetSymbolRetistrarStub(User2.KeyPair);
            User3SymbolRegistrarContractStub = GetSymbolRetistrarStub(User3.KeyPair);
            AdminAssociationContractStub =  GetAssociateContractTester(Admin.KeyPair);
            User1AssociationContractStub = GetAssociateContractTester(User1.KeyPair);
            User2AssociationContractStub = GetAssociateContractTester(User2.KeyPair);
            User3AssociationContractStub = GetAssociateContractTester(User3.KeyPair);

            
            AdminProxyAccountContractStubContractStub = GetProxyAccountContractStub(Admin.KeyPair);
            
            BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
        }


        private void DeploySaleContract(Account account)
        {
            var zeroContractStub = GetContractZeroTester(account.KeyPair);
            var result = AsyncHelper.RunSync(() => zeroContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(
                        File.ReadAllBytes(typeof(SymbolRegistrarContract).Assembly.Location))
                }));
            SymbolRegistrarContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            
            result = AsyncHelper.RunSync(() => zeroContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(
                        File.ReadAllBytes(typeof(MockProxyAccountContract.MockProxyAccountContract).Assembly.Location))
                }));
            ProxyAccountContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
        }
        
        
        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair senderKeyPair)
        {
            return GetTester<ACS0Container.ACS0Stub>(BasicContractZeroAddress, senderKeyPair);
        }
        
        internal SymbolRegistrarContractContainer.SymbolRegistrarContractStub GetSymbolRetistrarStub(ECKeyPair keyPair)
        {
            return GetTester<SymbolRegistrarContractContainer.SymbolRegistrarContractStub>(SymbolRegistrarContractAddress, keyPair);
        }

        internal ProxyAccountContractContainer.ProxyAccountContractStub GetProxyAccountContractStub(ECKeyPair keyPair)
        {
            return GetTester<ProxyAccountContractContainer.ProxyAccountContractStub>(ProxyAccountContractAddress, keyPair);
        }

        protected new async Task<IExecutionResult<Empty>> SubmitAndApproveProposalOfDefaultParliament(
            Address contractAddress,
            string methodName,
            IMessage message)
        {
            var defaultParliamentContractImplStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress, DefaultAccount.KeyPair);
            var defaultParliamentAddress = await defaultParliamentContractImplStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalId = await CreateProposalAsync(contractAddress,
                defaultParliamentAddress, methodName, message);
            await ApproveWithMinersAsync(proposalId);
            return await defaultParliamentContractImplStub.Release.SendAsync(proposalId);
        }
        
        private async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
            string methodName, IMessage input)
        {
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = contractAddress
            };
            
            var defaultParliamentContractImplStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress, DefaultAccount.KeyPair);
            var createResult = await defaultParliamentContractImplStub.CreateProposal.SendAsync(proposal);
            var proposalId = createResult.Output;
        
            return proposalId;
        }
        
        private async Task ApproveWithMinersAsync(Hash proposalId)
        {
            var miners = Accounts.Take(MinersCount).Select(a => a.KeyPair).ToList();
            foreach (var bp in miners)
            {
                var tester = GetParliamentContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            }
        }
        
        internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                keyPair);
        }

        //================================Association contract========================================================
        private async Task<Hash> CreateAssociationProposalAsync(Address contractAddress, Address organizationAddress,
                                                                string methodName, IMessage input, AssociationContractImplContainer.AssociationContractImplStub stub)
        {
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = contractAddress
            };
            var createResult = await stub.CreateProposal.SendAsync(proposal);
            var proposalId = createResult.Output;
            return proposalId;
        }


        private async Task ApproveAssociationWithMinersAsync(Hash proposalId)
        {
            var miners = Accounts.Take(MinersCount).Select(a => a.KeyPair).ToList();
            foreach (var bp in miners)
            {
                var tester = GetAssociateContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            }
        }


        internal async Task<IExecutionResult<Empty>> SubmitAndApproveProposalOfDefaultAssociation(
            Address contractAddress,
            string methodName,
            IMessage message,
            List<AssociationContractImplContainer.AssociationContractImplStub> stubs, Address defaultAssociationAddress)
        {
            var proposalId = await CreateAssociationProposalAsync(contractAddress,
                defaultAssociationAddress, methodName, message, stubs[0]);
            foreach (var stub in stubs)
            {
                var approveResult = await stub.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            }

            return await stubs[0].Release.SendAsync(proposalId);
        }


        internal AssociationContractImplContainer.AssociationContractImplStub GetAssociateContractTester(ECKeyPair keyPair)
        {
            return GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress,
                keyPair);
        }
        
    }
}