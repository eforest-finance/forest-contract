using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Forest
{
    /// <summary>
    /// The C# implementation of the contract defined in forest_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class ForestContract : ForestContractContainer.ForestContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.NFTContract.Value == null, "Already initialized.");
            State.NFTContract.Value = input.NftContractAddress;
            State.Admin.Value = input.AdminAddress ?? Context.Sender;
            State.ServiceFeeRate.Value = input.ServiceFeeRate == 0 ? DefaultServiceFeeRate : input.ServiceFeeRate;
            State.ServiceFeeReceiver.Value = input.ServiceFeeReceiver ?? State.Admin.Value;
            State.ServiceFee.Value = input.ServiceFee == 0 ? DefaultServiceFeeAmount : input.ServiceFee;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.GlobalTokenWhiteList.Value = new StringList
            {
                Value = {Context.Variables.NativeSymbol}
            };
            return new Empty();
        }

        public override Empty SetServiceFee(SetServiceFeeInput input)
        {
            AssertSenderIsAdmin();
            State.ServiceFeeRate.Value = input.ServiceFeeRate;
            State.ServiceFeeReceiver.Value = input.ServiceFeeReceiver ?? State.Admin.Value;
            return new Empty();
        }

        public override Empty SetGlobalTokenWhiteList(StringList input)
        {
            AssertSenderIsAdmin();
            if (!input.Value.Contains(Context.Variables.NativeSymbol))
            {
                input.Value.Add(Context.Variables.NativeSymbol);
            }
            State.GlobalTokenWhiteList.Value = input;
            Context.Fire(new GlobalTokenWhiteListChanged
            {
                TokenWhiteList = input
            });
            return new Empty();
        }

        public override Empty SetWhitelistContract(Address input)
        {
            AssertSenderIsAdmin();
            State.WhitelistContract.Value= input;
            return new Empty();
        }

        private void AssertWhitelistContractInitialized()
        {
            Assert(State.WhitelistContract.Value != null,"Whitelist Contract not initialized.");
        }

        private void AssertSenderIsAdmin()
        {
            AssertContractInitialized();
            Assert(Context.Sender == State.Admin.Value, "No permission.");
        }

        private void AssertContractInitialized()
        {
            Assert(State.Admin.Value != null, "Contract not initialized.");
        }
    }
}