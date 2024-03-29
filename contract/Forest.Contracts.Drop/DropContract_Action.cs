using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Forest.Contracts.Drop;

public partial class DropContract
{
    public override Empty CreateDrop(CreateDropInput input)
    {
        AssertInitialized();
        Assert(input != null, "Invalid input.");
        Assert(input.StartTime >= Context.CurrentBlockTime, "Invalid start time.");
        Assert(input.ExpireTime > input.StartTime, "Invalid expire time.");
        Assert(!string.IsNullOrWhiteSpace(input.CollectionSymbol), "Invalid collection symbol.");
        Assert(input.ClaimMax > 0, "Invalid claim max.");
        Assert(input.ClaimPrice is { Amount: >= 0 }, "Invalid claim price.");
        AssertSymbolExist(input.CollectionSymbol, SymbolType.NftCollection);
        Assert(input.ClaimPrice.Amount>=0, "Invalid claim price.");
        
        var dropId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.OriginTransactionId), HashHelper.ComputeFrom(Context.Sender));
        var dropInfo = new DropInfo
        {
            StartTime = input.StartTime,
            ExpireTime = input.ExpireTime,
            ClaimMax = input.ClaimMax,
            ClaimPrice = input.ClaimPrice,
            MaxIndex = 0,
            CurrentIndex = 0,
            TotalAmount = 0,
            ClaimAmount = 0,
            Owner = Context.Sender,
            IsBurn = input.IsBurn,
            State = DropState.Create,
            CollectionSymbol = input.CollectionSymbol,
            CreateTime = Context.CurrentBlockTime,
            UpdateTime = Context.CurrentBlockTime
        };
        State.DropInfoMap[dropId] = dropInfo;
        
        Context.Fire(new DropCreated
        {
            DropId = dropId,
            CollectionSymbol = dropInfo.CollectionSymbol,
            StartTime = dropInfo.StartTime,
            ExpireTime = dropInfo.ExpireTime,
            ClaimMax = dropInfo.ClaimMax,
            ClaimPrice = dropInfo.ClaimPrice,
            MaxIndex = dropInfo.MaxIndex,
            CurrentIndex = dropInfo.CurrentIndex,
            TotalAmount = dropInfo.TotalAmount,
            ClaimAmount = dropInfo.ClaimAmount,
            Owner = dropInfo.Owner,
            IsBurn = dropInfo.IsBurn,
            State = dropInfo.State,
            CreateTime = dropInfo.CreateTime,
            UpdateTime = dropInfo.UpdateTime
        });

        return new Empty();
    }

    public override Empty AddDropNFTDetailList(AddDropNFTDetailListInput input)
    {
        Assert(input != null, "Invalid input.");
        var inputDropDetailList = new DropDetailList();
        inputDropDetailList.Value.AddRange(input.Value.Distinct());
        
        var dropInfo = State.DropInfoMap[input.DropId];
        Assert(dropInfo != null, "Invalid drop id.");
        Assert(dropInfo.State == DropState.Create, "Invalid drop state.");
        Assert(dropInfo.Owner == Context.Sender, "Only owner can add drop detail list.");
        var collectionSymbol = dropInfo.CollectionSymbol;
        AssertDropDetailList(inputDropDetailList, dropInfo.CollectionSymbol, input.DropId, out var totalAmount);

        var maxDropDetailListCount = State.MaxDropDetailListCount.Value;
        var maxDropDetailIndexCount = State.MaxDropDetailIndexCount.Value;
        Assert(inputDropDetailList.Value.Count <= maxDropDetailListCount, "Invalid drop detail list count.");

        if (dropInfo.MaxIndex == 0)
        {
            State.DropDetailListMap[input.DropId][dropInfo.MaxIndex+1] = inputDropDetailList;
            dropInfo.MaxIndex = 1;
        }
        else
        {
            var lastDetailList = State.DropDetailListMap[input.DropId][dropInfo.MaxIndex];
            if ((lastDetailList.Value.Count + inputDropDetailList.Value.Count) <= maxDropDetailListCount)
            {
                lastDetailList.Value.AddRange(inputDropDetailList.Value);
                State.DropDetailListMap[input.DropId][dropInfo.MaxIndex] = lastDetailList;
            }else
            {
                var lastDetailListCount = lastDetailList.Value.Count;
                lastDetailList.Value.AddRange(inputDropDetailList.Value.ToList().GetRange(0, State.MaxDropDetailListCount.Value-lastDetailListCount));
                State.DropDetailListMap[input.DropId][dropInfo.MaxIndex] = lastDetailList;
                //write next index
                var nextIndex = dropInfo.MaxIndex + 1; 
                Assert(nextIndex <= maxDropDetailIndexCount, "Invalid total amount.");
                State.DropDetailListMap[input.DropId][nextIndex] = new DropDetailList() 
                { 
                    Value = { inputDropDetailList.Value.ToList().GetRange(State.MaxDropDetailListCount.Value-lastDetailListCount, inputDropDetailList.Value.Count)}
                };
                dropInfo.MaxIndex ++;
            }
        }
        
        dropInfo.TotalAmount += totalAmount;
        State.DropInfoMap[input.DropId] = dropInfo;
        
        //Fire drop-detail added
        Context.Fire(new DropDetailAdded
        {
            DropId = input.DropId,
            CollectionSymbol = collectionSymbol,
            DetailList = inputDropDetailList
        });
        
        //Fire drop changed
        Context.Fire(new DropChanged
        {
            DropId = input.DropId,
            MaxIndex = dropInfo.MaxIndex,
            CurrentIndex = dropInfo.CurrentIndex,
            TotalAmount = dropInfo.TotalAmount,
            ClaimAmount = dropInfo.ClaimAmount,
            UpdateTime = Context.CurrentBlockTime
        });
        Assert(dropInfo.MaxIndex <= maxDropDetailIndexCount, "Invalid max index.");
        return new Empty();
    }

    public override Empty SubmitDrop(Hash dropId)
    {
        Assert(dropId != null, "Invalid input.");
        var dropInfo = State.DropInfoMap[dropId];
        Assert(dropInfo != null, "Invalid drop id.");
        Assert(dropInfo.Owner == Context.Sender, "Only owner can submit drop.");
        Assert(dropInfo.State == DropState.Create, "Invalid drop state.");
        dropInfo.State = DropState.Submit;
        dropInfo.UpdateTime = Context.CurrentBlockTime;
        State.DropInfoMap[dropId] = dropInfo;
        
        Context.Fire(new DropStateChanged
        {
            DropId = dropId,
            State = dropInfo.State,
            UpdateTime = Context.CurrentBlockTime
        });
        return new Empty();
    }
    
    public override Empty CancelDrop(Hash dropId)
    {
        Assert(dropId != null, "Invalid input.");
        var dropInfo = State.DropInfoMap[dropId];
        Assert(dropInfo != null, "Invalid drop id.");
        Assert(dropInfo.Owner == Context.Sender || dropInfo.Owner == State.Admin.Value, "Only owner can submit drop.");
        Assert(dropInfo.State is DropState.Create or DropState.Submit, "Invalid drop state.");
        
        dropInfo.State = DropState.Cancel;
        dropInfo.UpdateTime = Context.CurrentBlockTime;
        State.DropInfoMap[dropId] = dropInfo;
        
        Context.Fire(new DropStateChanged
        {
            DropId = dropId,
            State = dropInfo.State,
            UpdateTime = Context.CurrentBlockTime
        });
        return new Empty();
    }
    
    public override Empty FinishDrop(FinishDropInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.DropId != null && input.Index > 0, "Invalid input.");
        var dropInfo = State.DropInfoMap[input.DropId];
        Assert(dropInfo != null, "Invalid drop id.");
        Assert(dropInfo.State != DropState.Finish, "Drop already finished.");
        Assert(dropInfo.ExpireTime <= Context.CurrentBlockTime, "Drop not expired.");

        var dropDetailInfo = State.DropDetailListMap[input.DropId][input.Index];
        Assert(dropDetailInfo.IsFinish == false, $"Drop detail index:{input.Index} already finished.");

        foreach (var detail in dropDetailInfo.Value)
        {
            if (detail.TotalAmount == detail.ClaimAmount) continue;
            if (dropInfo.IsBurn)
            {
                State.TokenContract.Burn.Send(new BurnInput
                {
                    Symbol = detail.Symbol,
                    Amount = detail.TotalAmount - detail.ClaimAmount,
                });
            }
            else
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    To = dropInfo.Owner,
                    Symbol = detail.Symbol,
                    Amount = detail.TotalAmount - detail.ClaimAmount
                });
            }
        }

        //change drop detail state to finish
        dropDetailInfo.IsFinish = true;
        State.DropDetailListMap[input.DropId][input.Index] = dropDetailInfo;
        Context.Fire(new DropDetailChanged
        {
            DropId = input.DropId,
            IsFinish = true,
            Index = input.Index
        });
        
        //change drop state to finish
        var isAllFinish = true;
        for (var i = 0; i < dropInfo.MaxIndex; i++)
        {
            if (i == input.Index) continue;
            
            var detail = State.DropDetailListMap[input.DropId][i];
            if(detail == null) continue;
            
            isAllFinish = isAllFinish && detail.IsFinish;
            if(!isAllFinish) break;
        }

        if (isAllFinish)
        {
            dropInfo.State = DropState.Finish;
            dropInfo.UpdateTime = Context.CurrentBlockTime;
            State.DropInfoMap[input.DropId] = dropInfo;
            Context.Fire(new DropStateChanged
            {
                DropId = input.DropId,
                State = dropInfo.State,
                UpdateTime = Context.CurrentBlockTime
            });
        }
        return new Empty();
    }

    public override Empty ClaimDrop(ClaimDropInput input)
    {
        Assert(input != null && input.DropId != null && input.ClaimAmount > 0, "Invalid input.");
        var dropInfo = State.DropInfoMap[input.DropId];
        Assert(dropInfo != null, "Invalid drop id.");
        Assert(!(dropInfo.State == DropState.Create), "Invalid drop state.");
        Assert(dropInfo.State == DropState.Submit, "The event has ended. You'll be automatically redirected to the Drops page.");
        Assert(dropInfo.ClaimAmount < dropInfo.TotalAmount, "All NFT already claimed.");
        Assert(dropInfo.ExpireTime > Context.CurrentBlockTime, "The event has ended.");
        Assert(dropInfo.StartTime <= Context.CurrentBlockTime, "The drop has not started yet.");
        Assert(Context.Sender != dropInfo.Owner, "Owner can't claim drop.");
        var claimDropDetail = State.ClaimDropMap[input.DropId][Context.Sender];
        Assert(input.ClaimAmount <= dropInfo.ClaimMax, "Claimed exceed max amount.");
        Assert(claimDropDetail == null || (claimDropDetail.Amount + input.ClaimAmount) <= dropInfo.ClaimMax,
            "Claimed exceed max amount.");
        var unClaimAmount = input.ClaimAmount;
        var currentIndex = dropInfo.CurrentIndex == 0 ? 1 : dropInfo.CurrentIndex;
        var claimDetailRecordList = new List<ClaimDetailRecord>();
        var claimDetailEventList = new List<ClaimDetail>();
        while (unClaimAmount >0 && currentIndex <= dropInfo.MaxIndex)
        {
            var currentClaimDropDetailList = State.DropDetailListMap[input.DropId][currentIndex];
            foreach (var detailInfo in currentClaimDropDetailList.Value)
            {
                if (detailInfo.ClaimAmount == detailInfo.TotalAmount) continue;
                //balance
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Symbol = detailInfo.Symbol,
                    Owner = Context.Self
                });
                
                if (balance.Balance < 0) break;
                
                var currentClaimAmount = 0L;
                if(unClaimAmount <= 0) break;
               
                if ((detailInfo.TotalAmount - detailInfo.ClaimAmount) >= unClaimAmount)
                {
                    currentClaimAmount = unClaimAmount;
                    detailInfo.ClaimAmount += currentClaimAmount;
                    unClaimAmount = 0;
                }
                else
                {
                    currentClaimAmount = detailInfo.TotalAmount - detailInfo.ClaimAmount;
                    detailInfo.ClaimAmount = detailInfo.TotalAmount;
                    unClaimAmount -= currentClaimAmount;
                }
                
                //transfer nft
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    To = Context.Sender,
                    Symbol = detailInfo.Symbol,
                    Amount = currentClaimAmount
                });
                claimDetailRecordList.Add(new ClaimDetailRecord
                {
                    Symbol = detailInfo.Symbol,
                    Amount = currentClaimAmount
                });
                
                //NFT info
                var symbolInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
                {
                    Symbol = detailInfo.Symbol
                });
                if (symbolInfo == null) break;
                claimDetailEventList.Add(new ClaimDetail
                {
                    Symbol = detailInfo.Symbol,
                    Amount = currentClaimAmount,
                    Name = symbolInfo.TokenName,
                    ChainId = symbolInfo.IssueChainId,
                    Image = symbolInfo.ExternalInfo == null ? "" : symbolInfo.ExternalInfo.Value.TryGetValue(DropContractConstants.NftImageUrlExternalInfoKey, out var imageUrl) ? imageUrl : "",
                });
                State.DropSymbolMap[input.DropId][detailInfo.Symbol] = 1;
            }
            //update claim drop detail
            State.DropDetailListMap[input.DropId][currentIndex] = currentClaimDropDetailList;
            if(currentIndex == dropInfo.MaxIndex) break;
            if(unClaimAmount > 0 && currentIndex < dropInfo.MaxIndex)
            {
                currentIndex++;
            }
        }

        //update drop info  
        dropInfo.ClaimAmount += input.ClaimAmount - unClaimAmount;
        dropInfo.CurrentIndex = Math.Min(currentIndex, dropInfo.MaxIndex);
        dropInfo.UpdateTime = Context.CurrentBlockTime;
        State.DropInfoMap[input.DropId] = dropInfo;

        if (claimDropDetail == null)
        {
            claimDropDetail = new ClaimDropDetail
            {
                Amount = input.ClaimAmount - unClaimAmount,
                UpdateTime = Context.CurrentBlockTime,
                CreateTime = Context.CurrentBlockTime
            };
        }
        else
        {
            claimDropDetail.Amount += input.ClaimAmount - unClaimAmount;
            claimDropDetail.UpdateTime = Context.CurrentBlockTime;
        }
        State.ClaimDropMap[input.DropId][Context.Sender] = claimDropDetail;

        //transfer token
        if (dropInfo.ClaimPrice.Amount > 0)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = dropInfo.Owner,
                Symbol = dropInfo.ClaimPrice.Symbol,
                Amount = dropInfo.ClaimPrice.Amount * (input.ClaimAmount - unClaimAmount)
            });
        }

        //Fire event drop changed
        Context.Fire(new DropChanged
        {
            DropId = input.DropId,
            MaxIndex = dropInfo.MaxIndex,
            CurrentIndex = dropInfo.CurrentIndex,
            TotalAmount = dropInfo.TotalAmount,
            ClaimAmount = dropInfo.ClaimAmount,
            UpdateTime = Context.CurrentBlockTime
        });

        //Fire event claim drop
        Context.Fire(new DropClaimAdded
        {
            DropId = input.DropId,
            CurrentAmount = input.ClaimAmount - unClaimAmount,
            Address = Context.Sender,
            TotalAmount = claimDropDetail.Amount,
            ClaimDetailList = new ClaimDetailList
            {
                Value = {claimDetailEventList}
            },
        });
        
        return new Empty();
    }
}