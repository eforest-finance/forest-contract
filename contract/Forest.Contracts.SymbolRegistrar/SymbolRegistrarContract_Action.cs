using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;

namespace Forest.Contracts.SymbolRegistrar
{
    /// <summary>
    /// The C# implementation of the contract defined in symbol_registrar_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class SymbolRegistrarContract : SymbolRegistrarContractContainer.SymbolRegistrarContractBase
    {
        
        public override Empty Buy(BuyInput input)
        {
            AssertContractInitialize();
            AssertSymbolPattern(input.Symbol);
            
            var specialSeed = State.SpecialSeedMap[input.Symbol];
            Assert(specialSeed == null, "Special seed " + input.Symbol + " not support deal.");
            CheckSymbolExisted(input.Symbol);

            var price = GetDealPrice(input.Symbol);
            Assert(price != null, "Symbol price not exits");

            var issueTo = input.IssueTo ?? Context.Sender;
            
            State.TokenContract.TransferFrom.Send(new TransferFromInput()
            {
                From = Context.Sender,
                To = State.ReceivingAccount.Value,
                Symbol = price.Symbol,
                Amount = price.Amount,
            });

            DoCreateSeed(issueTo, input.Symbol);

            Context.Fire(new Bought()
            {
                Buyer = Context.Sender,
                Symbol = input.Symbol,
                Price = new Price
                {
                    Symbol = price.Symbol,
                    Amount = price.Amount
                }
            });
            
            return new Empty();
        }
        
        public override Empty SetSeedExpirationConfig(SeedExpirationConfig input)
        {
            AssertInitialized();
            AssertAdmin();

            Assert(input != null, "Invalid input.");
            Assert(input.ExpirationTime > 0, "Invalid input expiration time.");

            if (State.SeedExpirationConfig.Value == input.ExpirationTime)
            {
                return new Empty();
            }

            State.SeedExpirationConfig.Value = input.ExpirationTime;

            Context.Fire(new SeedExpirationConfigChanged
            {
                SeedExpirationConfig = input
            });

            return new Empty();
        }

        public override Empty CreateSeed(CreateSeedInput input)
        {
            AssertSaleController();
            Assert(input.To != null && !input.To.Value.IsNullOrEmpty(), "To address is empty");
            AssertSymbolPattern(input.Symbol);
            var specialSeed = State.SpecialSeedMap[input.Symbol];
            Assert(specialSeed == null || specialSeed.SeedType != SeedType.Disable, "seed " + input.Symbol + " not support create.");
            CheckSymbolExisted(input.Symbol);
            DoCreateSeed(input.To, input.Symbol);
            return new Empty();
        }
        
        private void DoCreateSeed(Address to, string symbol, long expireTime = 0)
        {
            var createResult = CreateSeedToken(Context.Self, symbol, expireTime);
            if (!createResult)
            {
                return;
            }
            var seedSymbol = State.SymbolSeedMap[symbol];
            State.TokenContract.Issue.Send(
                new IssueInput
                {
                    Amount = 1,
                    Symbol = seedSymbol,
                    To = to
                });
            var seedInfo = State.SeedInfoMap[seedSymbol];
            seedInfo.To = to;
            State.SeedInfoMap[seedSymbol] = seedInfo;
            var specialSeed = State.SpecialSeedMap[symbol];
            Context.Fire(new SeedCreated
            {
                Symbol = seedSymbol,
                OwnedSymbol = symbol,
                ExpireTime = seedInfo.ExpireTime,
                SeedType = specialSeed?.SeedType ?? SeedType.Regular,
                To = to,
                ImageUrl = seedInfo.ImageUrl
            });
        }
        private void DoRenewSeed(string seedSymbol, long expireTime = 0)
        {
            if (State.TokenImplContract.Value == null)
            {
                State.TokenImplContract.Value = State.TokenContract.Value;
            }

            var renewInput = new ExtendSeedExpirationTimeInput
            {
                Symbol = seedSymbol,
                ExpirationTime = expireTime
            };
            
            State.ProxyAccountContract.ForwardCall.Send(
                new ForwardCallInput
                {
                    ContractAddress = State.TokenContract.Value,
                    MethodName = nameof(State.TokenImplContract.ExtendSeedExpirationTime),
                    ProxyAccountHash = GetProxyAccountHash(),
                    Args = renewInput.ToByteString()
                });
        }

        private bool CreateSeedToken(Address issuer, string symbol, long expireTime = 0)
        {
            var seedCollectionOwner = GetSeedCollectionOwner();
            State.LastSeedId.Value = State.LastSeedId.Value.Add(1);
            var seedSymbol = SymbolRegistrarContractConstants.SeedPrefix + State.LastSeedId.Value;
            var seedTokenInfo = GetTokenInfo(seedSymbol);
            for (var i = 1; i <= SymbolRegistrarContractConstants.MaxCycleCount; i++) {
                if (seedTokenInfo == null || string.IsNullOrWhiteSpace(seedTokenInfo.Symbol))
                {
                    break;
                }
                State.LastSeedId.Value = State.LastSeedId.Value.Add(1);
                seedSymbol = SymbolRegistrarContractConstants.SeedPrefix + State.LastSeedId.Value;
                seedTokenInfo = GetTokenInfo(seedSymbol);
            }
            if (seedTokenInfo != null && !string.IsNullOrWhiteSpace(seedTokenInfo.Symbol))
            {
                return false;
            }

            State.SymbolSeedMap[symbol] = seedSymbol;
            var createInput = new CreateInput
            {
                Symbol = seedSymbol,
                TokenName = SymbolRegistrarContractConstants.SeedPrefix + symbol,
                Decimals = 0,
                IsBurnable = true,
                TotalSupply = 1,
                Owner = seedCollectionOwner,
                Issuer = issuer,
                ExternalInfo = new ExternalInfo(),
                LockWhiteList = { State.TokenContract.Value }
            };
            
            createInput.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedOwnedSymbolExternalInfoKey] = symbol;
            expireTime = expireTime == 0 ? Context.CurrentBlockTime.AddSeconds(State.SeedExpirationConfig.Value).Seconds : expireTime;
            createInput.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedExpireTimeExternalInfoKey] = expireTime.ToString();
            
            if (!String.IsNullOrEmpty(State.SeedImageUrlPrefix.Value))
            {
                createInput.ExternalInfo.Value[SymbolRegistrarContractConstants.NftImageUrlExternalInfoKey] =
                    State.SeedImageUrlPrefix.Value + seedSymbol + SymbolRegistrarContractConstants.NftImageUrlSuffix;
            }

            State.ProxyAccountContract.ForwardCall.Send(
                new ForwardCallInput
                {
                    ContractAddress = State.TokenContract.Value,
                    MethodName = nameof(State.TokenContract.Create),
                    ProxyAccountHash = GetProxyAccountHash(),
                    Args = createInput.ToByteString()
                });
            State.SeedInfoMap[seedSymbol] = new SeedInfo
            {
                Symbol = seedSymbol,
                OwnedSymbol = symbol,
                ExpireTime = expireTime,
                ImageUrl = createInput.ExternalInfo.Value.GetValueOrDefault(SymbolRegistrarContractConstants.NftImageUrlExternalInfoKey, "")
            };
            return true;
        }

        private Address GetSeedCollectionOwner()
        {
            var owner = State.SeedCollectionOwner.Value;
            if (owner != null && !owner.Value.IsNullOrEmpty())
            {
                return owner;
            }
            var seedCollection = GetTokenInfo(SymbolRegistrarContractConstants.SeedPrefix +
                                              SymbolRegistrarContractConstants.CollectionSymbolSuffix);
            Assert(seedCollection?.Owner != null && !seedCollection.Owner.Value.IsNullOrEmpty(), "SeedCollection not existed.");
            State.SeedCollectionOwner.Value = seedCollection.Owner;
            return seedCollection.Owner;
        }

        private Hash GetProxyAccountHash()
        {
            var proxyAccountHash = State.ProxyAccountHash.Value;
            if (proxyAccountHash != null && !proxyAccountHash.Value.IsNullOrEmpty())
            {
                return proxyAccountHash;
            }
            var proxyAccount = State.ProxyAccountContract.GetProxyAccountByProxyAccountAddress.Call(GetSeedCollectionOwner());
            Assert(proxyAccount?.ProxyAccountHash != null, "ProxyAccountHash not existed.");
            State.ProxyAccountHash.Value = proxyAccount.ProxyAccountHash;
            return proxyAccount.ProxyAccountHash;
        }
        
        public override Empty RegularSeedRenew(RegularSeedRenewInput input)
        {
            AssertContractInitialize();
            AssertSeedSymbolPattern(input.SeedSymbol);
            CheckSeedBalanceExisted(input.SeedSymbol);
            var seedTokenInfo = GetTokenInfo(input.SeedSymbol);
            Assert(seedTokenInfo.Symbol.Length > 1, "Seed Symbol not exists");
            var seedOwnedSymbol = seedTokenInfo.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedOwnedSymbolExternalInfoKey];
            var seedExpTime = long.Parse(seedTokenInfo.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedExpireTimeExternalInfoKey]);
            Assert(!string.IsNullOrWhiteSpace(seedOwnedSymbol) && seedExpTime > Context.CurrentBlockTime.Seconds, "symbol seed not existed or expired");
            
            var specialSeed = State.SpecialSeedMap[seedOwnedSymbol];
            Assert(specialSeed == null, "Special seed " + input.SeedSymbol + " not support renew.");
            
            var price = GetDealPrice(seedOwnedSymbol);
            Assert(price != null, "Symbol price not exits");
            Assert(price.Symbol == input.Price.Symbol && price.Amount == input.Price.Amount, "input symbol price not correct");

            State.TokenContract.TransferFrom.Send(new TransferFromInput()
            {
                From = Context.Sender,
                To = State.ReceivingAccount.Value,
                Symbol = price.Symbol,
                Amount = price.Amount,
            });

            var nextExpTime = seedExpTime + State.SeedExpirationConfig.Value;
            DoRenewSeed(input.SeedSymbol, nextExpTime);

            Context.Fire(new SeedRenewed()
            {
                ChainId = Context.ChainId,
                Buyer = Context.Sender,
                Symbol = seedOwnedSymbol,
                SeedSymbol = input.SeedSymbol,
                ExpTime = nextExpTime,
                OriginalExpTime = seedExpTime,
                Price = new Price
                {
                    Symbol = input.Price.Symbol,
                    Amount = input.Price.Amount
                },
                SeedType = SeedType.Regular,
                RenewType = RenewType.Self
            });
            
            return new Empty();
        }
        
        public override Empty SpecialSeedRenew(SpecialSeedRenewInput input)
        {
            AssertContractInitialize();
            AssertSeedSymbolPattern(input.SeedSymbol);
            CheckSeedBalanceExisted(input.SeedSymbol);
            Assert(Context.Sender == input.Buyer, "param owner not sender");
            var seedTokenInfo = GetTokenInfo(input.SeedSymbol);
            Assert(seedTokenInfo.Symbol.Length > 1, "Seed Symbol not exists");
            var seedOwnedSymbol = seedTokenInfo.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedOwnedSymbolExternalInfoKey];
            var seedExpTime = long.Parse(seedTokenInfo.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedExpireTimeExternalInfoKey]);
            Assert(!string.IsNullOrWhiteSpace(seedOwnedSymbol) && seedExpTime > Context.CurrentBlockTime.Seconds, "symbol seed not existed or expired");
            
            var specialSeed = State.SpecialSeedMap[seedOwnedSymbol];
            Assert(specialSeed != null, "Not Special seed " + input.SeedSymbol + " not support renew.");
            var requestStr = string.Concat(input.Buyer.ToBase58(), input.SeedSymbol);
            requestStr = string.Concat(requestStr, input.Price.Symbol, input.Price.Amount, input.OpTime);
            CheckSeedRenewRequestHash(requestStr, input.RequestHash);
            var lastAddTime = State.SeedRenewTimeMap[input.SeedSymbol];
            Assert(input.OpTime > lastAddTime, "Invalid param OpTime");
            State.SeedRenewTimeMap[input.SeedSymbol] = input.OpTime;
            
            var price = input.Price;
            Assert(price != null, "Symbol price not exits");
            var priceTokenInfo = GetTokenInfo(price.Symbol);
            Assert(priceTokenInfo != null, "input price symbol not correct");
            Assert(price.Amount > 0, "input price amount not correct");
            
            State.TokenContract.TransferFrom.Send(new TransferFromInput()
            {
                From = Context.Sender,
                To = State.ReceivingAccount.Value,
                Symbol = price.Symbol,
                Amount = price.Amount,
            });

            var nextExpTime = seedExpTime + State.SeedExpirationConfig.Value;
            DoRenewSeed(input.SeedSymbol, nextExpTime);

            Context.Fire(new SeedRenewed()
            {
                ChainId = Context.ChainId,
                Buyer = Context.Sender,
                Symbol = seedOwnedSymbol,
                SeedSymbol = input.SeedSymbol,
                ExpTime = nextExpTime,
                OriginalExpTime = seedExpTime,
                Price = new Price
                {
                    Symbol = input.Price.Symbol,
                    Amount = input.Price.Amount
                },
                SeedType = SeedType.Unique,
                RenewType = RenewType.Self
            });
            
            return new Empty();
        }
        
        
         public override Empty BidFinishSeedRenew(BidFinishSeedRenewInput input)
        {
            AssertContractInitialize();
            AssertSeedSymbolPattern(input.SeedSymbol);
            CheckSeedBalanceExisted(input.SeedSymbol);
            var seedTokenInfo = GetTokenInfo(input.SeedSymbol);
            Assert(seedTokenInfo.Symbol.Length > 1, "Seed Symbol not exists");
            var seedOwnedSymbol = seedTokenInfo.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedOwnedSymbolExternalInfoKey];
            var seedExpTime = long.Parse(seedTokenInfo.ExternalInfo.Value[SymbolRegistrarContractConstants.SeedExpireTimeExternalInfoKey]);
            Assert(!string.IsNullOrWhiteSpace(seedOwnedSymbol) && seedExpTime > Context.CurrentBlockTime.Seconds, "symbol seed not existed or expired");
            Assert(input.BidFinishTime > seedExpTime && input.BidFinishTime < Context.CurrentBlockTime.Seconds, "bid finish time not correct");
            
            var specialSeed = State.SpecialSeedMap[seedOwnedSymbol];
            Assert(specialSeed != null, "Not Special seed " + input.SeedSymbol + " not support renew.");
            var requestStr = string.Concat(input.SeedSymbol, input.BidFinishTime, input.OpTime);
            CheckSeedRenewRequestHash(requestStr, input.RequestHash);
            var lastAddTime = State.SeedRenewTimeMap[input.SeedSymbol];
            Assert(input.OpTime > lastAddTime, "Invalid param OpTime");
            State.SeedRenewTimeMap[input.SeedSymbol] = input.OpTime;
            
            var nextExpTime = input.BidFinishTime + State.SeedExpirationConfig.Value;
            DoRenewSeed(input.SeedSymbol, nextExpTime);

            Context.Fire(new SeedRenewed()
            {
                ChainId = Context.ChainId,
                Buyer = Context.Sender,
                Symbol = seedOwnedSymbol,
                SeedSymbol = input.SeedSymbol,
                ExpTime = nextExpTime,
                OriginalExpTime = seedExpTime,
                SeedType = SeedType.Unique,
                RenewType = RenewType.Bid
            });
            
            return new Empty();
        }
        
        private void CheckSeedRenewRequestHash(string request, string hash)
        {
            var key = State.SeedRenewHashVerifyKey.Value;
            Assert(!string.IsNullOrEmpty(key), "Need SetTreePointsHashVerifyKey");
            var requestHash = HashHelper.ComputeFrom(string.Concat(request, key));
            Assert(hash.Equals(requestHash.ToHex()), "Unverified requests");
        }
        
    }
    
    
}