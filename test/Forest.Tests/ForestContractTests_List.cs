using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Forest.Whitelist;
using Shouldly;
using Xunit;

namespace Forest;

public class ForestContractListTests : ForestContractTestBase
{
    private const string NftSymbol = "TESTNFT-1";
    private const string ElfSymbol = "ELF";
    private const int ServiceFeeRate = 1000; // 10%
    private const long InitializeElfAmount = 10000_0000_0000;

    private async Task InitializeForestContract()
    {
        await AdminForestContractStub.Initialize.SendAsync(new InitializeInput
        {
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress,
            ServiceFeeRate = ServiceFeeRate,
        });

        await AdminForestContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);
    }

    private static Price Elf(long amunt)
    {
        return new Price()
        {
            Symbol = ElfSymbol,
            Amount = amunt
        };
    }

    private async Task PrepareNftData()
    {
        // create collections via MULTI-TOKEN-CONTRACT
        var executionResult = await UserTokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = "TESTNFT-0",
            TokenName = "TESTNFT—collection",
            TotalSupply = 100,
            Decimals = 0,
            Issuer = User1Address,
            IsBurnable = false,
            IssueChainId = 0,
            ExternalInfo = new ExternalInfo()
        });
        var log = TokenCreated.Parser
            .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(TokenCreated))
                .NonIndexed);
        log.Symbol.ShouldBe("TESTNFT-0");
        log.Decimals.ShouldBe(0);
        log.TotalSupply.ShouldBe(100);
        log.TokenName.ShouldBe("TESTNFT—collection");
        log.Issuer.ShouldBe(User1Address);
        log.IsBurnable.ShouldBe(false);
        log.IssueChainId.ShouldBe(9992731);

        // create NFT via MULTI-TOKEN-CONTRACT
        var executionResult1 = await UserTokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = NftSymbol,
            TokenName = NftSymbol,
            TotalSupply = 100,
            Decimals = 0,
            Issuer = User1Address,
            IsBurnable = false,
            IssueChainId = 0,
            ExternalInfo = new ExternalInfo()
        });
        var log1 = TokenCreated.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(TokenCreated))
                .NonIndexed);
        log1.Symbol.ShouldBe(NftSymbol);
        log1.Decimals.ShouldBe(0);
        log1.TotalSupply.ShouldBe(100);
        log1.TokenName.ShouldBe(NftSymbol);
        log1.Issuer.ShouldBe(User1Address);
        log1.IsBurnable.ShouldBe(false);
        log1.IssueChainId.ShouldBe(9992731);

        // issue 10 NFTs to self
        var executionResult2 = await UserTokenContractStub.Issue.SendAsync(new IssueInput()
        {
            Symbol = NftSymbol,
            Amount = 10,
            To = User1Address
        });
        var log2 = Issued.Parser
            .ParseFrom(executionResult2.TransactionResult.Logs.First(l => l.Name == nameof(Issued))
                .NonIndexed);
        log2.Symbol.ShouldBe(NftSymbol);
        log2.Amount.ShouldBe(10);
        log2.Memo.ShouldBe("");
        log2.To.ShouldBe(User1Address);
        // got 100-totalSupply and 10-supply
        var tokenInfo = await UserTokenContractStub.GetTokenInfo.SendAsync(new GetTokenInfoInput()
        {
            Symbol = NftSymbol,
        });

        tokenInfo.Output.TotalSupply.ShouldBe(100);
        tokenInfo.Output.Supply.ShouldBe(10);


        var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        nftBalance.Output.Balance.ShouldBe(10);


        // transfer thousand ELF to seller
        var executionResult3 =  await TokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User1Address,
            Symbol = ElfSymbol,
            Amount = 9999999
        });
        var log3 = Transferred.Parser
            .ParseFrom(executionResult3.TransactionResult.Logs.First(l => l.Name == nameof(Transferred))
                .NonIndexed); 
        log3.Amount.ShouldBe(9999999);
        
        // transfer thousand ELF to buyer
        await TokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User2Address,
            Symbol = ElfSymbol,
            Amount = 9999999
        });
    }

    [Fact]
    public async void ListWithFixedPrice1Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        var whitePrice = Elf(3);

        {
            var executionResult1 = await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(
                new ListWithFixedPriceInput
                {
                    Symbol = NftSymbol,
                    Quantity = 2,
                    IsWhitelistAvailable = true,
                    Price = sellPrice,
                    Whitelists = new WhitelistInfoList()
                    {
                        Whitelists =
                        {
                            new WhitelistInfo()
                            {
                                PriceTag = new PriceTagInfo()
                                {
                                    TagName = "WHITELIST_TAG",
                                    Price = whitePrice
                                },
                                AddressList = new AddressList()
                                {
                                    Value = { User2Address, User3Address },
                                }
                            },
                        }
                    },
                    Duration = new ListDuration
                    {
                        DurationHours = 24
                    }
                });
            var log1 = ListedNFTAdded.Parser
                .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTAdded))
                    .NonIndexed);
            log1.Owner.ShouldBe(User1Address);
            log1.Quantity.ShouldBe(2);
            log1.Symbol.ShouldBe(NftSymbol);
            log1.Price.Symbol.ShouldBe(ElfSymbol);
            log1.Price.Amount.ShouldBe(3);
            log1.Duration.StartTime.ShouldNotBeNull();
            log1.Duration.DurationHours.ShouldBe(24);
            
            var lo2 = FixedPriceNFTListed.Parser
                .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(FixedPriceNFTListed))
                    .NonIndexed);
            lo2.Owner.ShouldBe(User1Address);
            lo2.Quantity.ShouldBe(2);
            lo2.Symbol.ShouldBe(NftSymbol);
            lo2.Price.Symbol.ShouldBe(ElfSymbol);
            lo2.Price.Amount.ShouldBe(3);
            lo2.Duration.StartTime.ShouldNotBeNull();
            lo2.Duration.DurationHours.ShouldBe(24);
            

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(2);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(24);
        }

        //GetWhitelistId
        var whitelistId = (await Seller1ForestContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        })).WhitelistId;
        var whitelistPrice = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
            new GetExtraInfoByAddressInput
            {
                Address = User2Address,
                WhitelistId = whitelistId
            });
        whitelistPrice.TagName.ShouldBe("WHITELIST_TAG");

        {
            var whitelistInfo = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelistInfo.ExtraInfoIdList.Value.Single().AddressList.Value.Count.ShouldBe(2);
        }
    }

    [Fact]
    public async void ListWithFixedPrice2Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        var whitePrice = Elf(3);

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = false,
                Price = sellPrice,
                Whitelists = null,
                Duration = new ListDuration
                {
                    DurationHours = 24
                }
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(2);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(24);
        }

        //GetWhitelistId
        Func<Task> act = () => Seller1ForestContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        var exception = await Assert.ThrowsAsync<Exception>(act);
        exception.Message.ShouldContain("Failed to call GetWhitelistId");
    }

    [Fact]
    public async void ListWithFixedPrice3Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        var whitePrice = Elf(3);

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = false,
                Price = sellPrice,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            PriceTag = new PriceTagInfo()
                            {
                                TagName = "WHITELIST_TAG",
                                Price = whitePrice
                            },
                            AddressList = new AddressList()
                            {
                                Value = { User2Address, User3Address },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration
                {
                    DurationHours = 24
                }
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(2);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(24);
        }

        //GetWhitelistId
        Func<Task> act = () => Seller1ForestContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        var exception = await Assert.ThrowsAsync<Exception>(act);
        exception.Message.ShouldContain("Failed to call GetWhitelistId");
    }

    [Fact]
    public async void ListWithFixedPrice4Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = sellPrice,
                Duration = new ListDuration
                {
                    DurationHours = 24
                }
            });

            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = sellPrice,
                Duration = new ListDuration
                {
                    DurationHours = 24
                }
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(2);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(24);
        }
    }

    [Fact]
    public async void ListWithFixedPrice5Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(2);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }
    }

    [Fact]
    public async void ListWithFixedPrice6Test()
    {
        await InitializeForestContract();
        //await PrepareNftData();
        var sellPrice = Elf(3);
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = "oiii-1",
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("this NFT Info not exists.");
        }
        //Insufficient NFT balance.
    }

    [Fact]
    public async void ListWithFixedPrice7Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = new Price()
                {
                    Symbol = "usdt",
                    Amount = 33
                },
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("usdt is not in token white list.");
        }
    }

    [Fact]
    public async void ListWithFixedPrice8Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = new Price()
                {
                    Symbol = ElfSymbol,
                    Amount = -33
                },
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Incorrect listing price.");
            //
        }
    }

    [Fact]
    public async void ListWithFixedPrice9Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2,
                IsWhitelistAvailable = true,
                Price = new Price()
                {
                    Symbol = ElfSymbol,
                    Amount = 0
                },
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Incorrect listing price.");
            //
        }
    }

    [Fact]
    public async void ListWithFixedPrice10Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 2000,
                IsWhitelistAvailable = true,
                Price = new Price()
                {
                    Symbol = ElfSymbol,
                    Amount = 5
                },
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Check sender NFT balance failed.");
        }
    }

    [Fact]
    public async void ListWithFixedPrice11Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = -10,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Incorrect quantity.");
        }
    }

    [Fact]
    public async void ListWithFixedPrice12Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 0,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Incorrect quantity.");
        }
    }

    [Fact]
    public async void ListWithFixedPrice13Test()
    {
        //await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Contract not initialized.");
        }
    }

    [Fact]
    public async void ListWithFixedPrice14Test()
    {
        await AdminForestContractStub.Initialize.SendAsync(new InitializeInput
        {
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress,
            ServiceFeeRate = ServiceFeeRate,
        });

        //await AdminForestContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

        var sellPrice = Elf(3);
        {
            Func<Task> act = () => Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Whitelist Contract not initialized.");
        }
    }

    [Fact]
    public async void Delist15Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            var executionResult = await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(
                new ListWithFixedPriceInput
                {
                    Symbol = NftSymbol,
                    Quantity = 4,
                    IsWhitelistAvailable = true,
                    Price = sellPrice
                });
            var log = ListedNFTAdded.Parser
                .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTAdded))
                    .NonIndexed);
            log.Owner.ShouldBe(User1Address);
            log.Quantity.ShouldBe(4);
            log.Symbol.ShouldBe(NftSymbol);
            log.Price.Symbol.ShouldBe(ElfSymbol);
            log.Price.Amount.ShouldBe(3);
            log.Duration.StartTime.ShouldNotBeNull();
            log.Duration.DurationHours.ShouldBe(2147483647L);

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(4);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }

        var executionResult1 = await Seller1ForestContractStub.Delist.SendAsync(new DelistInput
        {
            Symbol = NftSymbol,
            Price = sellPrice,
            Quantity = 1
        });
        var log1 = ListedNFTChanged.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTChanged))
                .NonIndexed);
        log1.Owner.ShouldBe(User1Address);
        log1.Quantity.ShouldBe(3);
        log1.Symbol.ShouldBe(NftSymbol);
        log1.Price.Symbol.ShouldBe(ElfSymbol);
        log1.Price.Amount.ShouldBe(3);
        log1.Duration.StartTime.ShouldNotBeNull();
        log1.Duration.DurationHours.ShouldBe(2147483647L);
        
        var log2 = NFTDelisted.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.Last(l => l.Name == nameof(NFTDelisted))
                .NonIndexed);
        log2.Owner.ShouldBe(User1Address);
        log2.Quantity.ShouldBe(1);
        log2.Symbol.ShouldBe(NftSymbol);

        var listedNftInfo1 = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
            new GetListedNFTInfoListInput
            {
                Symbol = NftSymbol,
                Owner = User1Address
            })).Value.First();
        listedNftInfo1.Price.Symbol.ShouldBe("ELF");
        listedNftInfo1.Price.Amount.ShouldBe(3);
        listedNftInfo1.Quantity.ShouldBe(3);
        listedNftInfo1.ListType.ShouldBe(ListType.FixedPrice);
        listedNftInfo1.Duration.StartTime.ShouldNotBeNull();
        listedNftInfo1.Duration.DurationHours.ShouldBe(2147483647L);
    }


    [Fact]
    public async void Delist16Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }

        await Seller1ForestContractStub.Delist.SendAsync(new DelistInput
        {
            Symbol = NftSymbol,
            // // TokenId = 233,
            Price = sellPrice,
            Quantity = 1
        });
        {
            Func<Task> act = () => Seller1ForestContractStub.Delist.SendAsync(new DelistInput
            {
                Symbol = NftSymbol,
                // // TokenId = 233,
                Price = sellPrice,
                Quantity = 1
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Listed NFT Info not exists. (Or already delisted.");
        }
    }

    [Fact]
    public async void Delist17Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }
        {
            Func<Task> act = () => Seller1ForestContractStub.Delist.SendAsync(new DelistInput
            {
                Symbol = "abkd-1",
                Price = sellPrice,
                Quantity = 10000
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Listed NFT Info not exists. (Or already delisted.");
        }
    }

    [Fact]
    public async void Delist18Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }
        {
            Func<Task> act = () => Seller1ForestContractStub.Delist.SendAsync(new DelistInput
            {
                Symbol = NftSymbol,
                Price = null,
                Quantity = 1
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Need to specific list record.");
        }
    }

    [Fact]
    public async void Delist19Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }
        {
            Func<Task> act = () => Seller1ForestContractStub.Delist.SendAsync(new DelistInput
            {
                Symbol = NftSymbol,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 1000
                },
                Quantity = 1
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Listed NFT Info not exists. (Or already delisted.");
        }
    }

    [Fact]
    public async void Delist20Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }
        {
            Func<Task> act = () => Seller1ForestContractStub.Delist.SendAsync(new DelistInput
            {
                Symbol = NftSymbol,
                Price = sellPrice,
                Quantity = -9000
            });
            var exception = await Assert.ThrowsAsync<Exception>(act);
            exception.Message.ShouldContain("Quantity must be a positive integer.");
        }
    }


    [Fact]
    public async void DelistAllTest()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            var executionResult = await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(
                new ListWithFixedPriceInput
                {
                    Symbol = NftSymbol,
                    Quantity = 4,
                    IsWhitelistAvailable = true,
                    Price = sellPrice
                });
            var log = ListedNFTAdded.Parser
                .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTAdded))
                    .NonIndexed);
            log.Owner.ShouldBe(User1Address);
            log.Quantity.ShouldBe(4);
            log.Symbol.ShouldBe(NftSymbol);
            log.Price.Symbol.ShouldBe(ElfSymbol);
            log.Price.Amount.ShouldBe(3);
            log.Duration.StartTime.ShouldNotBeNull();
            log.Duration.DurationHours.ShouldBe(2147483647L);

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(4);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }

        var executionResult1 = await Seller1ForestContractStub.Delist.SendAsync(new DelistInput
        {
            Symbol = NftSymbol,
            Price = sellPrice,
            Quantity = 4
        });
        var log1 = ListedNFTRemoved.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTRemoved))
                .NonIndexed);
        log1.Owner.ShouldBe(User1Address);
        log1.Symbol.ShouldBe(NftSymbol);
        log1.Price.ShouldBeNull();
        log1.Duration.StartTime.ShouldNotBeNull();
        log1.Duration.DurationHours.ShouldBe(2147483647L);

        var log2 = NFTDelisted.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.Last(l => l.Name == nameof(NFTDelisted))
                .NonIndexed);
        log2.Owner.ShouldBe(User1Address);
        log2.Quantity.ShouldBe(4);
        log2.Symbol.ShouldBe(NftSymbol);

        var listedNftInfo1 = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
            new GetListedNFTInfoListInput
            {
                Symbol = NftSymbol,
                Owner = User1Address
            }));
        listedNftInfo1.Value.Count.ShouldBe(0);
    }

    [Fact]
    public async void Delist21Test()
    {
        await InitializeForestContract();
        await PrepareNftData();
        var sellPrice = Elf(3);
        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = NftSymbol,
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = sellPrice
            });

            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = User1Address
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(3);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(2147483647L);
        }
        await Seller1ForestContractStub.Delist.SendAsync(new DelistInput
        {
            Symbol = NftSymbol,
            Price = sellPrice,
            Quantity = 9000
        });

        var listedNftInfo1 = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
            new GetListedNFTInfoListInput
            {
                Symbol = NftSymbol,
                Owner = User1Address
            }));
        listedNftInfo1.Value.Count.ShouldBe(0);
    }

    [Fact]
    public async void TransferTest()
    {
        await InitializeForestContract();
        await PrepareNftData();
        {
            {
                var balance1 = await TokenContractStub.GetBalance.CallAsync(
                    new AElf.Contracts.MultiToken.GetBalanceInput
                    {
                        Symbol = NftSymbol,
                        Owner = User1Address
                    });

                var balance2 = await TokenContractStub.GetBalance.CallAsync(
                    new AElf.Contracts.MultiToken.GetBalanceInput
                    {
                        Symbol = NftSymbol,
                        Owner = User2Address
                    });

                var executionResult = await UserTokenContractStub.Transfer.SendAsync(new TransferInput()
                {
                    To = User2Address,
                    Symbol = NftSymbol,
                    Amount = 2,
                    Memo = "for you 2 nft ..."
                });
                var log1 = Transferred.Parser
                    .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(Transferred))
                        .NonIndexed);
                log1.Amount.ShouldBe(2);
                log1.Memo.ShouldBe("for you 2 nft ...");
        

                var balance3 = await TokenContractStub.GetBalance.CallAsync(
                    new AElf.Contracts.MultiToken.GetBalanceInput
                    {
                        Symbol = NftSymbol,
                        Owner = User2Address
                    });

                balance3.Balance.ShouldBe(2);
            }
        }
    }
}