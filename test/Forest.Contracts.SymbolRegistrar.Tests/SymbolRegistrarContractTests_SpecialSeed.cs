using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Forest.Contracts.SymbolRegistrar
{
    public class SymbolRegistrarContractTests_SpecialSeed : SymbolRegistrarContractTests
    {

        [Fact]
        public async Task SetSpecialSeed_notInit_notAuthor_fail()
        {
            // create proposal and approve
            var result = await Assert.ThrowsAsync<Exception>(() => SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress,
                "AddSpecialSeeds",
                new SpecialSeedList
                {
                    Value = { _specialUsd, _specialEth }
                }));
            result.Message.ShouldContain("No permission");
        }

        [Fact]
        public async Task SetSpecialSeed_notParliament_fail()
        {
            await InitializeContract();
            
            var result = await Assert.ThrowsAsync<Exception>(() => User1SymbolRegistrarContractStub.AddSpecialSeeds.SendAsync(new SpecialSeedList
                {
                    Value = { _specialUsd, _specialEth }
                }));
            result.Message.ShouldContain("No permission");
        }

        [Fact]
        public async Task SetSpecialSeed_removeSuccess()
        {
            await SetSpecialSeed_byProposal();

            // remove and add
            var addResult = await SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds",
                new SpecialSeedList
                {
                    Value = { _specialBtc }
                });
            addResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var removeResult = await SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress,
                "RemoveSpecialSeeds", new RemoveSpecialSeedInput
                {
                    Symbols = { _specialUsd.Symbol }
                });
            removeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // logs
            var logEvent =
                removeResult.TransactionResult.Logs.First(log => log.Name.Contains(nameof(SpecialSeedRemoved)));
            var specialSeedRemoved = SpecialSeedRemoved.Parser.ParseFrom(logEvent.NonIndexed);
            specialSeedRemoved.RemoveList.Value.Count.ShouldBe(1);

            // query seed list and verify
            var seedUsd = await AdminSymbolRegistrarContractStub.GetSpecialSeed.CallAsync(new StringValue
            {
                Value = _specialUsd.Symbol
            });
            seedUsd.Symbol.ShouldBe(string.Empty);


            var seedEth = await AdminSymbolRegistrarContractStub.GetSpecialSeed.CallAsync(new StringValue
            {
                Value = _specialEth.Symbol
            });
            seedEth.Symbol.ShouldBe(_specialEth.Symbol);

            var seedBtc = await AdminSymbolRegistrarContractStub.GetSpecialSeed.CallAsync(new StringValue
            {
                Value = _specialBtc.Symbol
            });
            seedBtc.Symbol.ShouldBe(_specialBtc.Symbol);
        }


        [Fact]
        public async Task SetSpecialSeed_fail()
        {
            await InitializeContract();

            // Price symbol not exists
            var notExits = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd_errorPrice, _specialEth }
                })
            );
            notExits.Message.ShouldContain("not exists");

            // Invalid issue chain
            var invalidIssueChain = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd, _specialEth_noIssueChainId }
                })
            );
            invalidIssueChain.Message.ShouldContain("Invalid issue chain");


            // long name
            var invalidSymbolLength = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd, _specialLongName }
                })
            );
            invalidSymbolLength.Message.ShouldContain("Invalid symbol length");

            // invalid symbol
            var invalidSymbol = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd, _specialInvalidSymbol }
                })
            );
            invalidSymbol.Message.ShouldContain("Invalid symbol");

            // invalid NFT symbol
            var invalidNftSymbol = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd, _specialInvalidNftSymbol }
                })
            );
            invalidNftSymbol.Message.ShouldContain("Invalid nft symbol");


            // Invalid issue chain contract
            var invalidIssueChainContract = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd, _specialEth_noIssueChainContractAddress }
                })
            );
            invalidIssueChainContract.Message.ShouldContain("Invalid issue chain contract");

            // duplicate symbol
            var duplicateSymbol = await Assert.ThrowsAsync<Exception>(() =>
                SubmitAndApproveProposalOfDefaultParliament(SymbolRegistrarContractAddress, "AddSpecialSeeds", new SpecialSeedList
                {
                    Value = { _specialUsd, _specialUsd, _specialEth }
                })
            );
            duplicateSymbol.Message.ShouldContain("Duplicate symbol");
        }
    }
}