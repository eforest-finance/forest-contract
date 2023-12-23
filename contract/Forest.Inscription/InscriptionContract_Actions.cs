﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Forest.Inscription;

public partial class InscriptionContract : InscriptionContractContainer.InscriptionContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        // Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.ConfigurationContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConfigurationContractSystemName);
        State.Admin.Value = input.Admin;
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty ChangeAdmin(Address input)
    {
        Assert(Context.Sender == State.Admin.Value,"No permission.");
        State.Admin.Value = input;
        return new Empty();
    }


    public override Empty DeployInscription(DeployInscriptionInput input)
    {
        Assert(!string.IsNullOrWhiteSpace(input.SeedSymbol) && !string.IsNullOrWhiteSpace(input.Tick) &&
               input.Max > 0 && input.Limit > 0, "Invalid input.");
        Assert(!string.IsNullOrWhiteSpace(input.Image) && Encoding.UTF8.GetByteCount(input.Image) <=
            InscriptionContractConstants.ImageMaxLength, "Invalid image data.");
        var tick = input.Tick?.ToUpper();
        // Approve Seed.
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            Symbol = input.SeedSymbol,
            From = Context.Sender,
            To = Context.Self,
            Amount = 1,
        });

        var issueChainId = State.IssueChainId.Value == 0
            ? InscriptionContractConstants.IssueChainId
            : State.IssueChainId.Value;
        
        // Create collection
        var collectionExternalInfo =
            GenerateExternalInfo(tick, input.Max, input.Limit, input.Image, SymbolType.NftCollection);
        var collectionSymbol = CreateInscription(tick, input.Max, issueChainId, collectionExternalInfo,
            SymbolType.NftCollection);

        // Create nft item
        var nftExternalInfo =
            GenerateExternalInfo(tick, input.Max, input.Limit, input.Image, SymbolType.Nft);
        var nftSymbol = CreateInscription(tick, input.Max, issueChainId, nftExternalInfo, SymbolType.Nft);
        State.InscribedLimit[tick] = input.Limit;

        Context.Fire(new InscriptionCreated
        {
            CollectionSymbol = collectionSymbol,
            ItemSymbol = nftSymbol,
            Tick = tick,
            TotalSupply = input.Max,
            Decimals = InscriptionContractConstants.InscriptionDecimals,
            Issuer = Context.Self,
            IsBurnable = true,
            IssueChainId = issueChainId,
            CollectionExternalInfo = new ExternalInfos
            {
                Value = { collectionExternalInfo.Value }
            },
            ItemExternalInfo = new ExternalInfos
            {
                Value = { nftExternalInfo.Value }
            },
            Owner = Context.Self,
            Deployer = Context.Sender,
            Limit = input.Limit
        });

        return new Empty();
    }

    public override Empty IssueInscription(IssueInscriptionInput input)
    {
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input.");
        var tick = input.Tick?.ToUpper();
        var symbol = GetNftSymbol(tick);
        var collectionSymbol = GetCollectionSymbol(tick);
        var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = collectionSymbol
        });
        Assert(tokenInfo != null, $"Token not exist.{tokenInfo?.Symbol}");
        var distributors = GenerateDistributors(tick);
        var info = DeployInscriptionInfo.Parser.ParseJson(
            tokenInfo?.ExternalInfo.Value[InscriptionContractConstants.InscriptionDeployKey]);
        Assert(long.TryParse(info.Lim, out var lim), "Invalid inscription limit.");
        State.InscribedLimit[tick] = lim;
        IssueAndModifyBalance(tick,symbol,tokenInfo.TotalSupply, distributors.Values.ToList());

        Context.Fire(new InscriptionIssued
        {
            Symbol = symbol,
            Tick = tick,
            Amt = tokenInfo.TotalSupply,
            To = Context.Self,
            InscriptionInfo = info.ToString()
        });

        return new Empty();
    }


    public override Empty Inscribe(InscribedInput input)
    {
        var tick = input.Tick?.ToUpper();
        var tokenInfo = CheckInputAndGetSymbol(tick, input.Amt);
        SelectDistributorAndTransfer(tick, tokenInfo.Symbol, input.Amt,InscribeType.Parallel);
        Context.Fire(new InscriptionTransferred
        {
            From = Context.Self,
            Symbol = tokenInfo.Symbol,
            Tick = tick,
            Amt = input.Amt,
            To = Context.Sender,
            InscriptionInfo = tokenInfo.ExternalInfo.Value[InscriptionContractConstants.InscriptionMintKey]
        });
        return new Empty();
    }
    
    public override Empty MintInscription(InscribedInput input)
    {
        var tick = input.Tick?.ToUpper();
        var tokenInfo = CheckInputAndGetSymbol(tick, input.Amt);
        SelectDistributorAndTransfer(tick, tokenInfo.Symbol, input.Amt,InscribeType.NotParallel);
        Context.Fire(new InscriptionTransferred
        {
            From = Context.Self,
            Symbol = tokenInfo.Symbol,
            Tick = tick,
            Amt = input.Amt,
            To = Context.Sender,
            InscriptionInfo = tokenInfo.ExternalInfo.Value[InscriptionContractConstants.InscriptionMintKey]
        });
        return new Empty();
    }

    public override Empty SetIssueChainId(Int32Value input)
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
        Assert(input != null && input.Value > 0, "Invalid input.");
        State.IssueChainId.Value = input.Value;
        return new Empty();
    }

    public override Int32Value GetIssueChainId(Empty input)
    {
        return new Int32Value
        {
            Value = State.IssueChainId.Value
        };
    }

    public override Empty SetDistributorCount(Int32Value input)
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
        Assert(input != null && input.Value > 0, "Invalid input.");
        State.DistributorCount.Value = input.Value;
        return new Empty();
    }

    public override Int32Value GetDistributorCount(Empty input)
    {
        return new Int32Value
        {
            Value = State.DistributorCount.Value
        };
    }
}