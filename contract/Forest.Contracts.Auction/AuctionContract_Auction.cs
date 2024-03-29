using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Forest.Contracts.Auction;

public partial class AuctionContract
{
    public override Empty CreateAuction(CreateAuctionInput input)
    {
        AssertInitialized();
        AssertAuctionControllerPermission();
        ValidateCreateAuctionInput(input);

        var auctionId = GenerateAuctionId(input.Symbol);
        Assert(State.AuctionInfoMap[auctionId] == null, "Auction already exist.");

        var auctionInfo = Extensions.CreateAuctionInfo(input, Context.Sender).SetAuctionId(auctionId);

        if (auctionInfo.IsStartImmediately())
        {
            auctionInfo.SetAuctionTime(Context.CurrentBlockTime);
        }

        State.AuctionInfoMap[auctionId] = auctionInfo;

        TransferTokenFromCreator(AuctionContractConstants.DefaultAmount, auctionInfo.Symbol);

        Context.Fire(auctionInfo.GenerateAuctionCreatedEvent());

        return new Empty();
    }

    public override Empty PlaceBid(PlaceBidInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.AuctionId != null && !input.AuctionId.Value.IsNullOrEmpty(), "Invalid input auction id.");

        var auctionInfo = State.AuctionInfoMap[input.AuctionId];

        Assert(auctionInfo != null, "Auction not exist.");

        switch (auctionInfo.AuctionType)
        {
            case AuctionType.English:
                PlaceBidForEnglishAuction(input, auctionInfo);
                break;
            default:
                Assert(false, "Unsupported auction type.");
                break;
        }

        Context.Fire(auctionInfo.GenerateBidPlacedEvent());

        return new Empty();
    }

    public override Empty Claim(ClaimInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.AuctionId != null && !input.AuctionId.Value.IsNullOrEmpty(), "Invalid input auction id.");

        var auctionInfo = State.AuctionInfoMap[input.AuctionId];
        Assert(auctionInfo != null, "Auction not exist.");
        Assert(auctionInfo.StartTime != null, "Auction not start yet.");
        Assert(auctionInfo.FinishTime == null, "Auction already claimed.");

        var currentBlockTime = Context.CurrentBlockTime;
        Assert(auctionInfo.IsAuctionFinished(currentBlockTime), "Auction not end yet.");

        auctionInfo.SetFinishTime(currentBlockTime);

        if (auctionInfo.IsAuctionBid())
        {
            TransferTokenToBidder(auctionInfo);
            TransferToReceivingAccount(auctionInfo);
        }
        // No one bid, refund to creator
        else
        {
            RefundTokenToCreator(auctionInfo);
        }

        Context.Fire(auctionInfo.GenerateClaimedEvent());

        return new Empty();
    }

    private void PlaceBidForEnglishAuction(PlaceBidInput input, AuctionInfo auctionInfo)
    {
        var currentBlockTime = Context.CurrentBlockTime;

        if (!auctionInfo.IsAuctionStarted())
        {
            auctionInfo.SetAuctionTime(currentBlockTime);

            Context.Fire(auctionInfo.GenerateAuctionTimeUpdatedEvent());
        }

        Assert(!auctionInfo.IsAuctionFinished(currentBlockTime), "Auction finished. Bid failed.");

        var bidInfo = auctionInfo.LastBidInfo;

        var auctionConfig = auctionInfo.AuctionConfig;
        AssertBidPriceEnough(bidInfo?.Bidder == null ? auctionInfo.StartPrice.Amount : bidInfo.Price.Amount,
            input.Amount, auctionConfig.MinMarkup);

        RefundToLastBidder(bidInfo);

        bidInfo = new BidInfo
        {
            Bidder = Context.Sender,
            Price = new Price
            {
                Amount = input.Amount,
                Symbol = auctionInfo.StartPrice.Symbol
            },
            BidTime = currentBlockTime
        };

        TransferFromBidder(bidInfo);

        // Extend auction end time when bid in countdown time
        if (auctionInfo.IsInCountdownTime(currentBlockTime))
        {
            auctionInfo.ExtendEndTime();
            Context.Fire(auctionInfo.GenerateAuctionTimeUpdatedEvent());
        }

        State.AuctionInfoMap[input.AuctionId] = auctionInfo.UpdateBidInfo(bidInfo);
    }

    private void RefundToLastBidder(BidInfo bidInfo)
    {
        if (bidInfo != null)
        {
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = bidInfo.Price.Symbol,
                Amount = bidInfo.Price.Amount,
                To = bidInfo.Bidder,
                Memo = "Refund"
            });
        }
    }

    private void TransferTokenFromCreator(long amount, string symbol)
    {
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = Context.Self,
            Amount = amount,
            Symbol = symbol,
            Memo = "Auction"
        });
    }

    private void RefundTokenToCreator(AuctionInfo auctionInfo)
    {
        State.TokenContract.Transfer.Send(new TransferInput
        {
            Symbol = auctionInfo.Symbol,
            Amount = AuctionContractConstants.DefaultAmount,
            To = auctionInfo.Creator,
            Memo = "Auction"
        });
    }

    private void TransferFromBidder(BidInfo bidInfo)
    {
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = bidInfo.Bidder,
            To = Context.Self,
            Amount = bidInfo.Price.Amount,
            Symbol = bidInfo.Price.Symbol,
            Memo = "Auction"
        });
    }

    private void TransferTokenToBidder(AuctionInfo auctionInfo)
    {
        State.TokenContract.Transfer.Send(new TransferInput
        {
            Symbol = auctionInfo.Symbol,
            Amount = AuctionContractConstants.DefaultAmount,
            To = auctionInfo.LastBidInfo.Bidder,
            Memo = "Auction"
        });
    }

    private void TransferToReceivingAccount(AuctionInfo auctionInfo)
    {
        State.TokenContract.Transfer.Send(new TransferInput
        {
            Symbol = auctionInfo.LastBidInfo.Price.Symbol,
            Amount = auctionInfo.LastBidInfo.Price.Amount,
            To = auctionInfo.ReceivingAddress,
            Memo = "Auction"
        });
    }
}