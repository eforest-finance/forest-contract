using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Forest.Contracts.SymbolRegistrar
{
    public class SymbolRegistrarContractTests_Admin : SymbolRegistrarContractTests
    {
        

        [Fact]
        public async Task InitTest_view()
        {
            await InitializeContract();
            
            var bizConfig = AdminSymbolRegistrarContractStub.GetBizConfig.CallAsync(new Empty());
            bizConfig.Result.ReceivingAccount.ShouldBe(Admin.Address);
            bizConfig.Result.AdministratorAddress.ShouldBe(Admin.Address);
        }

        [Fact]
        public async Task InitTest_Fail()
        {
            // no permission
            var exception = await Assert.ThrowsAsync<Exception>(() => User1SymbolRegistrarContractStub.Initialize.SendAsync(
                new InitializeInput()
                {
                    ReceivingAccount = Admin.Address
                }));
            exception.ShouldNotBeNull();
            exception.Message.ShouldContain("No permission");

            // invalid param
            var exception2 = await Assert.ThrowsAsync<Exception>(() => AdminSymbolRegistrarContractStub.Initialize.SendAsync(
                new InitializeInput()
                {
                    // no param
                }));
            exception2.ShouldNotBeNull();
            exception2.Message.ShouldContain("PaymentReceiverAddress required");

            // invalid param
            var exception3 = await Assert.ThrowsAsync<Exception>(() => AdminSymbolRegistrarContractStub.Initialize.SendAsync(
                new InitializeInput()
                {
                    ReceivingAccount = new Address()
                }));
            exception3.ShouldNotBeNull();
            exception3.Message.ShouldContain("PaymentReceiverAddress required");

            // success
            await InitializeContract();

            // has bean init
            var exception4 = await Assert.ThrowsAsync<Exception>(InitializeContract);
            exception4.ShouldNotBeNull();
            exception4.Message.ShouldContain("has bean Initialized");
        }

        [Fact]
        public async Task SetTest_success()
        {
            var exception1 = await Assert.ThrowsAsync<Exception>(() =>
                AdminSymbolRegistrarContractStub.SetReceivingAccount.SendAsync(User2.Address));
            exception1.ShouldNotBeNull();
            exception1.Message.ShouldContain("Contract not Initialized");
            
            await InitTest_view();
            
            var exception2 = await Assert.ThrowsAsync<Exception>(() =>
                AdminSymbolRegistrarContractStub.SetAdmin.SendAsync(new Address()));
            exception2.ShouldNotBeNull();
            exception2.Message.ShouldContain("Invalid param");
            
            var exception3 = await Assert.ThrowsAsync<Exception>(() =>
                AdminSymbolRegistrarContractStub.SetReceivingAccount.SendAsync(new Address()));
            exception3.ShouldNotBeNull();
            exception3.Message.ShouldContain("Invalid param");

            await AdminSymbolRegistrarContractStub.SetReceivingAccount.SendAsync(User2.Address);
            await AdminSymbolRegistrarContractStub.SetAdmin.SendAsync(User1.Address);

            var bizConfig = AdminSymbolRegistrarContractStub.GetBizConfig.CallAsync(new Empty());
            bizConfig.Result.ReceivingAccount.ShouldBe(User2.Address);
            bizConfig.Result.AdministratorAddress.ShouldBe(User1.Address);
        }

        [Fact]
        public async Task InitWithSpecialSeed_success()
        {
            var result = await AdminSymbolRegistrarContractStub.Initialize.SendAsync(new InitializeInput()
            {
                ReceivingAccount = Admin.Address,
                SpecialSeeds = new SpecialSeedList
                {
                    Value = { _specialUsd, _specialEth }
                }
            });
            
            // logs
            var logEvent = result.TransactionResult.Logs.First(log => log.Name.Contains(nameof(SpecialSeedAdded)));
            var specialSeedAdded = SpecialSeedAdded.Parser.ParseFrom(logEvent.NonIndexed);
            specialSeedAdded.AddList.Value.Count.ShouldBe(2);

            // query seed list and verify
            var seedUsd = await AdminSymbolRegistrarContractStub.GetSpecialSeed.CallAsync(new StringValue
            {
                Value = _specialUsd.Symbol
            });
            seedUsd.Symbol.ShouldBe(_specialUsd.Symbol);
            
            
            var seedEth = await AdminSymbolRegistrarContractStub.GetSpecialSeed.CallAsync(new StringValue
            {
                Value = _specialEth.Symbol
            });
            seedEth.Symbol.ShouldBe(_specialEth.Symbol);
        }

        [Fact]
        public async Task SetTest_fail()
        {
            await InitTest_view();

            var exception1 = await Assert.ThrowsAsync<Exception>(() =>
                User1SymbolRegistrarContractStub.SetAdmin.SendAsync(User1.Address));
            exception1.ShouldNotBeNull();
            exception1.Message.ShouldContain("No permission");

            var exception2 = await Assert.ThrowsAsync<Exception>(() =>
                User1SymbolRegistrarContractStub.SetReceivingAccount.SendAsync(User2.Address));
            exception2.ShouldNotBeNull();
            exception2.Message.ShouldContain("No permission");
        }
    }
}