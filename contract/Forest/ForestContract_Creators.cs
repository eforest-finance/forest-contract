using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Forest;

public partial class ForestContract
{
    public override Empty SetRoyalty(SetRoyaltyInput input)
    {
        AssertContractInitialized();
        AssertSenderIsAdmin();
        Assert(0 <= input.Royalty && input.Royalty < FeeDenominator, "Royalty should be between 0% to 100%.");
        var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = input.Symbol,
        });
        Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), "TokenInfo not found.");

        State.RoyaltyInfoMap[input.Symbol] = new RoyaltyInfo()
        {
            RoyaltyFeeReceiver = input.RoyaltyFeeReceiver,
            Royalty = input.Royalty
        };
        return new Empty();
    }

    public override Empty SetTokenWhiteList(SetTokenWhiteListInput input)
    {
        AssertContractInitialized();
        Assert(
            input.TokenWhiteList.Value.Count > 0 &&
            input.TokenWhiteList.Value.Count <= State.BizConfig.Value.MaxTokenWhitelistCount,
            $"TokenWhiteList length should be between 1-{State.BizConfig.Value.MaxTokenWhitelistCount}");

        var nftCollectionInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = input.Symbol
        });

        foreach (var symbol in input.TokenWhiteList.Value)
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = symbol
            });
            Assert(tokenInfo?.Symbol?.Length > 0, "Invalid token : " + symbol);
        }

        Assert(nftCollectionInfo.Issuer != null, "NFT Collection not found.");
        Assert(nftCollectionInfo.Issuer == Context.Sender, "Only NFT Collection Creator can set token white list.");

        State.TokenWhiteListMap[input.Symbol] = input.TokenWhiteList;
        Context.Fire(new TokenWhiteListChanged
        {
            Symbol = input.Symbol,
            TokenWhiteList = input.TokenWhiteList
        });
        return new Empty();
    }
}