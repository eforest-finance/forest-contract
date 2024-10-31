using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Forest;

public partial class ForestContract
{
    public override Empty AddTreePoints(AddTreePointsInput input)
    {
        AssertContractInitialized();
        Assert(input != null, "Invalid param");
        Assert(!input.Address.Value.IsNullOrEmpty(), "Invalid param Address");
        Assert(input.Points > 0, "Invalid param Points");
        Assert(!string.IsNullOrEmpty(input.RequestHash), "Invalid param RequestHash");
        Assert(input.OpTime != null && input.OpTime > 0, "Invalid param OpTime");
        Assert(Context.Sender == input.Address, "Param Address is not Sender");
        
        var requestStr = string.Concat(input.Address, input.Points, input.PointsType, input.OpTime);
        CheckPointsRequestHash(requestStr, input.RequestHash);
        
        var lastAddTime = State.TreePointsAddTimeMap[input.Address];
        Assert(input.OpTime > lastAddTime, "Invalid param OpTime");

        var treePointsInfo = State.TreePointsMap[input.Address];
        if (treePointsInfo != null)
        {
            treePointsInfo.Points += input.Points;
        }
        else
        {
            treePointsInfo = new TreePointsInfo()
            {
                Owner = input.Address,
                Points = input.PointsType
            };
        }
        
        State.TreePointsMap[input.Address] = treePointsInfo;
        State.TreePointsAddTimeMap[input.Address] = input.OpTime;
        
        //event
        Context.Fire(new TreePointsAdded()
        {
            Owner = input.Address,
            Points = input.Points,
            PointsType = input.PointsType,
            OpTime = input.OpTime,
            TotalPoints = treePointsInfo.Points,
            RequestHash = input.RequestHash
        });
        return new Empty();
    }
    public override Empty ClaimTreePoints(ClaimTreePointsInput input)
    {
        AssertContractInitialized();
        Assert(input != null, "Invalid param");
        Assert(!input.Address.Value.IsNullOrEmpty(), "Invalid param Address");
        Assert(input.Points > 0, "Invalid param Points");
        Assert(!string.IsNullOrEmpty(input.ActivityId), "Invalid param ActivityId");
        Assert(!string.IsNullOrEmpty(input.RequestHash), "Invalid param RequestHash");
        Assert(input.OpTime != null && input.OpTime > 0, "Invalid param OpTime");
        Assert(input.Reward != null && !string.IsNullOrEmpty(input.Reward.Symbol) && input.Reward.Amount > 0, "Param Address is not Sender");
        Assert(Context.Sender == input.Address, "Param Address is not Sender");
        
        var requestStr = string.Concat(input.Address, input.Points, input.PointsType, input.OpTime);
        requestStr = string.Concat(requestStr, input.ActivityId, input.Reward.Symbol, input.Reward.Amount);
        CheckPointsRequestHash(requestStr, input.RequestHash);
        
        var lastOpTime = State.TreePointsActivityClaimTimeMap[input.Address][input.ActivityId];
        Assert(input.OpTime > lastOpTime, "Invalid param OpTime");

        var treePointsInfo = State.TreePointsMap[input.Address];
        Assert(treePointsInfo != null, "your points is zero");
        Assert(treePointsInfo.Points >= input.Points, "You don't have enough points");
        treePointsInfo.Points -= input.Points;
        State.TreePointsMap[input.Address] = treePointsInfo;
        State.TreePointsActivityClaimTimeMap[input.Address][input.ActivityId] = input.OpTime;
        
        //transfer award
        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Symbol = input.Reward.Symbol,
            Owner = Context.Self
        });
        Assert(balance.Balance >= input.Reward.Amount,$"The platform does not have enough {input.Reward.Symbol}");
        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = input.Address,
            Symbol = input.Reward.Symbol,
            Amount = input.Reward.Amount
        });
        //event
        Context.Fire(new TreePointsClaimed()
        {
            Owner = input.Address,
            Points = input.Points,
            ActivityId = input.ActivityId,
            OpTime = input.OpTime,
            RewardSymbol = input.Reward.Symbol,
            RewardAmount = input.Reward.Amount,
            TotalPoints = treePointsInfo.Points,
            RequestHash = input.RequestHash
        });
        return new Empty();
    }
    public override Empty SetTreePointsHashVerifyKey(StringValue input)
    {
        AssertContractInitialized();
        Assert(input != null && !string.IsNullOrEmpty(input.Value), "Invalid param key");
        var key = State.TreePointsHashVerifyKey.Value;
        if (string.IsNullOrEmpty(key))
        {
            State.TreePointsHashVerifyKey.Value = input.Value;
            return new Empty();
        }
        AssertSenderIsAdmin();
        State.TreePointsHashVerifyKey.Value = input.Value;
        return new Empty();
    }
    
    public override Empty TreeLevelUpgrade(TreeLevelUpgradeInput input)
    {
        AssertContractInitialized();
        Assert(input != null, "Invalid param");
        Assert(!input.Address.Value.IsNullOrEmpty(), "Invalid param Address");
        Assert(input.Points > 0, "Invalid param Points");
        Assert(!string.IsNullOrEmpty(input.RequestHash), "Invalid param RequestHash");
        Assert(input.OpTime != null && input.OpTime > 0, "Invalid param OpTime");
        Assert(input.UpgradeLevel > 0, $"Invalid UpgradeLevel, Should be greater than 0");
        Assert(Context.Sender == input.Address, "Param Address is not Sender");
        
        var requestStr = string.Concat(input.Address, input.Points, input.OpTime, input.UpgradeLevel);
        CheckPointsRequestHash(requestStr, input.RequestHash);
        
        var lastOpTime = State.TreePointsLevelUpgradeTimeMap[input.Address];
        Assert(input.OpTime > lastOpTime, "Invalid param OpTime");

        var treePointsInfo = State.TreePointsMap[input.Address];
        Assert(treePointsInfo != null, "your points is zero");
        Assert(treePointsInfo.Points >= input.Points, "You don't have enough points");
        treePointsInfo.Points -= input.Points;
        State.TreePointsMap[input.Address] = treePointsInfo;
        State.TreePointsLevelUpgradeTimeMap[input.Address] = input.OpTime;

        //event
        Context.Fire(new TreeLevelUpgraded()
        {
            Owner = input.Address,
            Points = input.Points,
            OpTime = input.OpTime,
            UpgradeLevel = input.UpgradeLevel,
            TotalPoints = treePointsInfo.Points,
            RequestHash = input.RequestHash
        });
        return new Empty();
    }
}