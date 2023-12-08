using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Forest.Services;
using Forest.Whitelist;
using Google.Protobuf.WellKnownTypes;

namespace Forest;

public partial class ForestContract
{
    /// <summary>
    /// There are 2 types of making offer.
    /// 1. Aiming a owner.
    /// 2. Only aiming nft. Owner will be the nft protocol creator.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty MakeOffer(MakeOfferInput input)
    {
        AssertContractInitialized();
        Assert(input.Quantity > 0, "Invalid param Quantity.");
        Assert(input.Price.Amount > 0, "Invalid price amount.");
        var nftInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = input.Symbol,
        });
        Assert(nftInfo != null, "Invalid symbol data");
        Assert(input.Quantity <= nftInfo?.TotalSupply, "Offer quantity beyond totalSupply");

        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Symbol = input.Price.Symbol,
            Owner = Context.Sender
        });
        Assert(balance.Balance >= input.Price.Amount * input.Quantity, "Insufficient funds");
        var originBalance = balance.Clone();

        var amount = GetOfferTotalAmount(Context.Sender, input.Price.Symbol);
        var allowance = GetAllowance(Context.Sender, input.Price.Symbol);
        var totalAmount = amount.Add(input.Price.Amount.Mul(input.Quantity));
        Assert(allowance >= totalAmount, $"The allowance you set is less than required. Please reset it.");
        
        var tokenWhiteList = GetTokenWhiteList(input.Symbol).Value;
        Assert(tokenWhiteList.Contains(input.Price.Symbol), $"Price symbol {input.Price.Symbol} not available");

        var makeOfferService = GetMakeOfferService();
        makeOfferService.ValidateOffer(input);

        if (nftInfo.Supply != 0 && input.OfferTo == null)
        {
            input.OfferTo = nftInfo.Issuer;
        }

        var blockTime = Context.CurrentBlockTime;
        var sender = Context.Sender;
        var listedNftInfoList = State.ListedNFTInfoListMap[input.Symbol][input.OfferTo];

        var whitelistManager = GetWhitelistManager();

        if (makeOfferService.IsSenderInWhitelist(input, out var whitelistId) &&
            whitelistManager.IsWhitelistAvailable(whitelistId))
        {
            // Deal one NFT with whitelist price.
            var price = whitelistManager.GetExtraInfoByAddress(whitelistId);
            if (price != null && price.Amount <= input.Price.Amount && price.Symbol == input.Price.Symbol)
            {
                var minStartList = listedNftInfoList.Value
                    .Where(info => blockTime <= info.Duration.StartTime.AddHours(info.Duration.DurationHours))
                    .OrderBy(i => i.Duration.StartTime)
                    .ToList();
                if (minStartList.Count == 0)
                {
                    PerformMakeOffer(input, originBalance);
                    return new Empty();
                }

                if (blockTime < minStartList[0].Duration.StartTime)
                {
                    PerformMakeOffer(input, originBalance);
                    return new Empty();
                }

                if (TryDealWithFixedPriceWhitelist(input, price, whitelistId))
                {
                    minStartList[0].Quantity = minStartList[0].Quantity.Sub(1);
                    if (minStartList[0].Quantity == 0)
                    {
                        listedNftInfoList.Value.Remove(minStartList[0]);
                        Context.Fire(new ListedNFTRemoved
                        {
                            Symbol = minStartList[0].Symbol,
                            Duration = minStartList[0].Duration,
                            Owner = minStartList[0].Owner,
                            Price = minStartList[0].Price
                        });
                    }
                    else
                    {
                        Context.Fire(new ListedNFTChanged
                        {
                            Symbol = minStartList[0].Symbol,
                            Duration = minStartList[0].Duration,
                            Owner = minStartList[0].Owner,
                            PreviousDuration = minStartList[0].Duration,
                            Quantity = minStartList[0].Quantity,
                            Price = minStartList[0].Price,
                            WhitelistId = whitelistId
                        });
                    }
                }

                input.Quantity = input.Quantity.Sub(1);
                if (input.Quantity == 0)
                {
                    return new Empty();
                }
            }
        }

        var dealStatus = makeOfferService.GetDealStatus(input, out var affordableNftInfoList);
        switch (dealStatus)
        {
            case DealStatus.NotDeal:
                PerformMakeOffer(input, originBalance);
                return new Empty();
        }

        Assert(nftInfo.Supply > 0, "NFT does not exist.");

        if (listedNftInfoList.Value.All(i => i.ListType != ListType.FixedPrice))
        {
            PerformMakeOffer(input, originBalance);
            State.ListedNFTInfoListMap[input.Symbol][input.OfferTo] = listedNftInfoList;
            return new Empty();
        }

        var dealService = GetDealService();
        var getDealResultListInput = new GetDealResultListInput
        {
            MakeOfferInput = input,
            ListedNftInfoList = new ListedNFTInfoList
            {
                Value = { affordableNftInfoList }
            }
        };
        var normalPriceDealResultList = dealService.GetDealResultList(getDealResultListInput).ToList();
        if (normalPriceDealResultList.Count == 0)
        {
            PerformMakeOffer(input, originBalance);
            return new Empty();
        }

        var toRemove = new ListedNFTInfoList();
        foreach (var dealResult in normalPriceDealResultList)
        {
            var listedNftInfo = affordableNftInfoList[dealResult.Index];
            if (!TryDealWithFixedPrice(sender, input, dealResult, listedNftInfo, out var dealQuantity)) continue;
            dealResult.Quantity = dealResult.Quantity.Sub(dealQuantity);
            listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(dealQuantity);
            input.Quantity = input.Quantity.Sub(dealQuantity);
            if (listedNftInfo.Quantity == 0)
            {
                toRemove.Value.Add(listedNftInfo);
            }
            else
            {
                Context.Fire(new ListedNFTChanged
                {
                    Symbol = listedNftInfo.Symbol,
                    Duration = listedNftInfo.Duration,
                    Owner = listedNftInfo.Owner,
                    PreviousDuration = listedNftInfo.Duration,
                    Quantity = listedNftInfo.Quantity,
                    Price = listedNftInfo.Price,
                    WhitelistId = whitelistId
                });
            }
        }

        if (toRemove.Value.Count != 0)
        {
            foreach (var info in toRemove.Value)
            {
                listedNftInfoList.Value.Remove(info);
                Context.Fire(new ListedNFTRemoved
                {
                    Symbol = info.Symbol,
                    Duration = info.Duration,
                    Owner = info.Owner,
                    Price = info.Price
                });
            }
        }

        if (input.Quantity > 0)
        {
            PerformMakeOffer(input, originBalance);
        }

        State.ListedNFTInfoListMap[input.Symbol][input.OfferTo] = listedNftInfoList;

        return new Empty();
    }

    /// <summary>
    /// Batch buy
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty BatchBuyNow(BatchBuyNowInput input)
    {
        AssertContractInitialized();
        Assert(input != null && input.FixPriceList.Any(), "Invalid input data");
        var nftInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = input.Symbol,
        });
        Assert(nftInfo != null, "Invalid symbol data");
        var untradedList = new List<FixPriceList>();
        foreach (var fixPrice in input.FixPriceList)
        {
            var result = SingleMakeOfferForBatchBuyNow(input.Symbol, new FixPriceList()
            {
                OfferTo = fixPrice.OfferTo,
                Quantity = fixPrice.Quantity,
                Price = fixPrice.Price,
                StartTime = fixPrice.StartTime
            });
            untradedList.Add(result);
        }

        return new Empty();
    }

    private FixPriceList SingleMakeOfferForBatchBuyNow(string symbol, FixPriceList input)
    {
        Assert(input.Quantity > 0, "Invalid param Quantity.");
        Assert(input.Price.Amount > 0, "Invalid price amount.");
        Assert(input.OfferTo != null, "Invalid param OfferTo.");
        var nftInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = symbol,
        });
        Assert(nftInfo != null, "Invalid symbol data");
        Assert(input.Quantity <= nftInfo?.TotalSupply, "Offer quantity beyond totalSupply");

        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Symbol = input.Price.Symbol,
            Owner = Context.Sender
        });
        Assert(balance.Balance >= input.Price.Amount * input.Quantity, "Insufficient funds");

        var makeOfferService = GetMakeOfferService();
        makeOfferService.ValidateFixPriceList(input);
        
        var sender = Context.Sender;
        var listedNftInfoList = State.ListedNFTInfoListMap[symbol][input.OfferTo];

        makeOfferService.GetAffordableNftInfoList(symbol, input, out var affordableNftInfoList);
        Assert(affordableNftInfoList != null, "affordableNftInfoList doen not exist");
        Assert(nftInfo.Supply > 0, "NFT does not exist.");

        var dealService = GetDealService();
        var normalPriceDealResultList = dealService.GetDealResultListForBatchBuy(symbol, input,
            new ListedNFTInfoList
            {
                Value = { affordableNftInfoList }
            }
            , out long needToDealQuantity).ToList();
        Assert(normalPriceDealResultList.Count > 0, "NormalPrice does not exist.");

        var toRemove = new ListedNFTInfoList();
        foreach (var dealResult in normalPriceDealResultList)
        {
            var listedNftInfo = affordableNftInfoList[dealResult.Index];

            if (!TryDealWithFixedPriceForBatch(sender, symbol, input, dealResult
                    , listedNftInfo, out var dealQuantity))
                continue;
            listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(dealQuantity);
            input.Quantity = input.Quantity.Sub(dealQuantity);
            if (listedNftInfo.Quantity == 0)
            {
                toRemove.Value.Add(listedNftInfo);
            }
            else
            {
                Context.Fire(new ListedNFTChanged
                {
                    Symbol = listedNftInfo.Symbol,
                    Duration = listedNftInfo.Duration,
                    Owner = listedNftInfo.Owner,
                    PreviousDuration = listedNftInfo.Duration,
                    Quantity = listedNftInfo.Quantity,
                    Price = listedNftInfo.Price,
                });
            }
        }

        if (toRemove.Value.Count != 0)
        {
            foreach (var info in toRemove.Value)
            {
                listedNftInfoList.Value.Remove(info);
                Context.Fire(new ListedNFTRemoved
                {
                    Symbol = info.Symbol,
                    Duration = info.Duration,
                    Owner = info.Owner,
                    Price = info.Price
                });
            }
        }

        State.ListedNFTInfoListMap[symbol][input.OfferTo] = listedNftInfoList;
        return input;
    }

    private bool TryDealWithFixedPriceWhitelist(MakeOfferInput input, Price price, Hash whitelistId)
    {
        Assert(input.Price.Symbol == price.Symbol, $"Need to use token {price.Symbol}, not {input.Price.Symbol}");
        //Get extraInfoId according to the sender.
        var extraInfoId = State.WhitelistContract.GetTagIdByAddress.Call(new GetTagIdByAddressInput()
        {
            WhitelistId = whitelistId,
            Address = Context.Sender
        });
        State.WhitelistContract.RemoveAddressInfoListFromWhitelist.Send(new RemoveAddressInfoListFromWhitelistInput()
        {
            WhitelistId = whitelistId,
            ExtraInfoIdList = new ExtraInfoIdList()
            {
                Value =
                {
                    new ExtraInfoId
                    {
                        AddressList = new Whitelist.AddressList { Value = { Context.Sender } },
                        Id = extraInfoId
                    }
                }
            }
        });
        var totalAmount = price.Amount.Mul(1);
        PerformDeal(new PerformDealInput
        {
            NFTFrom = input.OfferTo,
            NFTTo = Context.Sender,
            NFTSymbol = input.Symbol,
            NFTQuantity = 1,
            PurchaseSymbol = price.Symbol,
            PurchaseAmount = totalAmount,
        });
        return true;
    }

    public override Empty CancelOffer(CancelOfferInput input)
    {
        AssertContractInitialized();

        OfferList offerList;
        var newOfferList = new OfferList();
        var cancelOfferMap = new Dictionary<string, long>();
        
        // Admin can remove expired offer.
        if (input.OfferFrom != null && input.OfferFrom != Context.Sender)
        {
            AssertSenderIsAdmin();

            offerList = State.OfferListMap[input.Symbol][input.OfferFrom];

            if (offerList != null)
            {
                
                foreach (var offer in offerList.Value)
                {
                    if (offer.ExpireTime >= Context.CurrentBlockTime)
                    {
                        newOfferList.Value.Add(offer);
                    }
                    else
                    {
                        var amount = cancelOfferMap.TryGetValue(offer.Price.Symbol, out var value)
                            ? value
                            : 0;
                        cancelOfferMap[offer.Price.Symbol] = amount + offer.Quantity.Mul(offer.Price.Amount);
                        Context.Fire(new OfferRemoved
                        {
                            Symbol = input.Symbol,
                            OfferFrom = offer.From,
                            OfferTo = offer.To,
                            ExpireTime = offer.ExpireTime
                        });
                    }
                }

                State.OfferListMap[input.Symbol][input.OfferFrom] = newOfferList;
                foreach (var cancelOffer in cancelOfferMap)
                {
                    ModifyOfferTotalAmount(input.OfferFrom, cancelOffer.Key, -cancelOffer.Value);
                }
                
            }

            return new Empty();
        }

        //owner can remove select offer.

        offerList = State.OfferListMap[input.Symbol][Context.Sender];

        var nftInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = input.Symbol,
        });
        if (nftInfo.Issuer == null)
        {
            // This nft does not exist.
            State.OfferListMap[input.Symbol].Remove(Context.Sender);
        }

        Assert(offerList?.Value?.Count > 0, "Offer not exists");

        if (input.IndexList != null && input.IndexList.Value.Any())
        {
            for (var i = 0; i < offerList?.Value?.Count; i++)
            {
                if (!input.IndexList.Value.Contains(i))
                {
                    newOfferList.Value.Add(offerList.Value[i]);
                }
                else
                {
                    var amount = cancelOfferMap.TryGetValue(offerList.Value[i].Price.Symbol, out var value)
                        ? value
                        : 0;
                    cancelOfferMap[offerList.Value[i].Price.Symbol] = amount + offerList.Value[i].Quantity.Mul(offerList.Value[i].Price.Amount);
                    Context.Fire(new OfferRemoved
                    {
                        Symbol = input.Symbol,
                        OfferFrom = Context.Sender,
                        OfferTo = offerList.Value[i].To,
                        ExpireTime = offerList.Value[i].ExpireTime
                    });
                }
            }
            foreach (var cancelOffer in cancelOfferMap)
            {
                ModifyOfferTotalAmount(input.OfferFrom, cancelOffer.Key, -cancelOffer.Value);
            }
            Context.Fire(new OfferCanceled
            {
                Symbol = input.Symbol,
                OfferFrom = Context.Sender,
                IndexList = input.IndexList
            });
        }
        else
        {
            newOfferList.Value.Add(offerList.Value);
        }

        State.OfferListMap[input.Symbol][Context.Sender] = newOfferList;
       
        return new Empty();
    }


    /// <summary>
    /// Sender is buyer.
    /// </summary>
    private bool TryDealWithFixedPrice(Address sender, MakeOfferInput input, DealResult dealResult,
        ListedNFTInfo listedNftInfo, out long actualQuantity)
    {
        var usePrice = input.Price.Clone();
        usePrice.Amount = Math.Min(input.Price.Amount, dealResult.PurchaseAmount);
        actualQuantity = Math.Min(input.Quantity, listedNftInfo.Quantity);

        var totalAmount = usePrice.Amount.Mul(actualQuantity);
        PerformDeal(new PerformDealInput
        {
            NFTFrom = input.OfferTo,
            NFTTo = sender,
            NFTSymbol = input.Symbol,
            NFTQuantity = actualQuantity,
            PurchaseSymbol = usePrice.Symbol,
            PurchaseAmount = totalAmount,
        });
        return true;
    }
    
    /// <summary>
    /// Sender is buyer.
    /// </summary>
    private bool TryDealWithFixedPriceForBatch(Address sender, string symbol, FixPriceList input, DealResult dealResult,
        ListedNFTInfo listedNftInfo, out long actualQuantity)
    {
        var senderBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Symbol = symbol,
            Owner = input.OfferTo
        });
        if (senderBalance == null || senderBalance.Balance == 0)
        {
            actualQuantity = 0;
            return false;
        } 
        var usePrice = input.Price.Clone();
        usePrice.Amount = Math.Min(input.Price.Amount, dealResult.PurchaseAmount);
        actualQuantity = Math.Min(input.Quantity, listedNftInfo.Quantity);
        actualQuantity = Math.Min(actualQuantity, senderBalance.Balance);
        
        if (actualQuantity == 0)
        {
            return false;
        } 
        
        var totalAmount = usePrice.Amount.Mul(actualQuantity);
        PerformDeal(new PerformDealInput
        {
            NFTFrom = input.OfferTo,
            NFTTo = sender,
            NFTSymbol = symbol,
            NFTQuantity = actualQuantity,
            PurchaseSymbol = usePrice.Symbol,
            PurchaseAmount = totalAmount,
        });
        return true;
    }

    /// <summary>
    /// Will go to Offer List.
    /// </summary>
    /// <param name="input"></param>
    private void PerformMakeOffer(MakeOfferInput input, GetBalanceOutput originGetBalanceOutput)
    {
        var offerList = State.OfferListMap[input.Symbol][Context.Sender] ?? new OfferList();
        Assert(offerList.Value.Count < State.BizConfig.Value.MaxOfferCount,
            $"The number of offers you can make on this NFT item has reached the maximum ({State.BizConfig.Value.MaxOfferCount}).");
        var expireTime = input.ExpireTime ?? Context.CurrentBlockTime.AddDays(DefaultExpireDays);
        var maybeSameOffer = offerList.Value.SingleOrDefault(o =>
            o.Price.Symbol == input.Price.Symbol && o.Price.Amount == input.Price.Amount &&
            o.ExpireTime == expireTime && o.To == input.OfferTo && o.From == Context.Sender);
        if (maybeSameOffer == null)
        {
            offerList.Value.Add(new Offer
            {
                From = Context.Sender,
                To = input.OfferTo,
                Price = input.Price,
                ExpireTime = expireTime,
                Quantity = input.Quantity
            });
            Context.Fire(new OfferAdded
            {
                Symbol = input.Symbol,
                OfferFrom = Context.Sender,
                OfferTo = input.OfferTo,
                ExpireTime = expireTime,
                Price = input.Price,
                Quantity = input.Quantity,
                OriginBalance = originGetBalanceOutput?.Balance ?? 0,
                OriginBalanceSymbol = originGetBalanceOutput?.Symbol ?? ""
            });
        }
        else
        {
            maybeSameOffer.Quantity = maybeSameOffer.Quantity.Add(input.Quantity);
            Context.Fire(new OfferChanged
            {
                Symbol = input.Symbol,
                OfferFrom = Context.Sender,
                OfferTo = input.OfferTo,
                Price = input.Price,
                ExpireTime = expireTime,
                Quantity = maybeSameOffer.Quantity
            });
        }

        State.OfferListMap[input.Symbol][Context.Sender] = offerList;
        ModifyOfferTotalAmount(Context.Sender, input.Price.Symbol, input.Price.Amount.Mul(input.Quantity));
        var addressList = State.OfferAddressListMap[input.Symbol] ?? new AddressList();

        if (!addressList.Value.Contains(Context.Sender))
        {
            addressList.Value.Add(Context.Sender);
            State.OfferAddressListMap[input.Symbol] = addressList;
        }

        Context.Fire(new OfferMade
        {
            Symbol = input.Symbol,
            OfferFrom = Context.Sender,
            OfferTo = input.OfferTo,
            ExpireTime = expireTime,
            Price = input.Price,
            Quantity = input.Quantity
        });
    }
}