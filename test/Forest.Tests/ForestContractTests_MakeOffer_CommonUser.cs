using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Forest;

public partial class ForestContractMakeOfferTests
{

        
    [Fact]
    public async void MakeOffer_Case35_commonUser_beforeStartTime_notDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // offerPrice < whitePrice < sellPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(10_0000_0000);
        
        // before startTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion

        #region approve transfer

        {
            // approve contract handle NFT of seller   
            await UserTokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Symbol = NftSymbol,
                Amount = 5,
                Spender = ForestContractAddress
            });

            // approve contract handle ELF of buyer   
            await User2TokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Symbol = ElfSymbol,
                Amount = InitializeElfAmount,
                Spender = ForestContractAddress
            });
        }

        #endregion

        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 1,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
        }

        #endregion

        #region check seller NFT, not deal

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);
        }

        #endregion
        
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBeGreaterThan(0);
            offerList.Value[0].To.ShouldBe(User1Address);
            offerList.Value[0].From.ShouldBe(User2Address);
        }

        #endregion

    }
    
        
    [Fact]
    public async void MakeOffer_Case36_commonUser_beforePublicTime_notDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // offerPrice < whitePrice < sellPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(10_0000_0000);
        
        // before publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion

        #region approve transfer

        {
            // approve contract handle NFT of seller   
            await UserTokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Symbol = NftSymbol,
                Amount = 5,
                Spender = ForestContractAddress
            });

            // approve contract handle ELF of buyer   
            await User2TokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Symbol = ElfSymbol,
                Amount = InitializeElfAmount,
                Spender = ForestContractAddress
            });
        }

        #endregion

        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 1,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
        }

        #endregion

        #region check seller NFT, not deal

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);
        }

        #endregion
        
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBeGreaterThan(0);
            offerList.Value[0].To.ShouldBe(User1Address);
            offerList.Value[0].From.ShouldBe(User2Address);
        }

        #endregion

    }
    
            
    [Fact]
    public async void MakeOffer_Case37_commonUser_beforePublicTime_notDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice <= offerPrice < sellPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(3_0000_0000);
        
        // before publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion

        #region approve transfer

        {
            // approve contract handle NFT of seller   
            await UserTokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Symbol = NftSymbol,
                Amount = 5,
                Spender = ForestContractAddress
            });

            // approve contract handle ELF of buyer   
            await User2TokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Symbol = ElfSymbol,
                Amount = InitializeElfAmount,
                Spender = ForestContractAddress
            });
        }

        #endregion

        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 1,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
        }

        #endregion

        #region check seller NFT, not deal

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);
        }

        #endregion
        
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBeGreaterThan(0);
            offerList.Value[0].To.ShouldBe(User1Address);
            offerList.Value[0].From.ShouldBe(User2Address);
        }

        #endregion

    }
    
    [Fact]
    public async void MakeOffer_Case38_commonUser_beforePublicTime_notDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice <= offerPrice < sellPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(3_0000_0000);
        
        // after publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion

        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 1,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
        }

        #endregion

        #region check seller NFT, not deal

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);
        }

        #endregion
        
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBeGreaterThan(0);
            offerList.Value[0].To.ShouldBe(User1Address);
            offerList.Value[0].From.ShouldBe(User2Address);
        }

        #endregion

    }
    
    [Fact]
    public async void MakeOffer_Case39_commonUser_beforePublicTime_deal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice < sellPrice = offerPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(5_0000_0000);
        var serviceFee = sellPrice.Amount * ServiceFeeRate / 10000;
        
        // after publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion
        
        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 1,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
        }

        #endregion
        
        #region check seller NFT

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(9);
        }

        #endregion

        #region check buyer NFT

        {
            var nftBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User2Address
            });
            nftBalance.Output.Balance.ShouldBe(1);
        }

        #endregion

        #region check service fee

        {
            // check buyer ELF balance
            var user1ElfBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User1Address
            });
            user1ElfBalance.Output.Balance.ShouldBe(InitializeElfAmount + sellPrice.Amount - serviceFee);
        }

        #endregion
    }
    
    [Fact]
    public async void MakeOffer_Case40_commonUser_beforePublicTime_deal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice < sellPrice < offerPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(10_0000_0000);
        var serviceFee = sellPrice.Amount * ServiceFeeRate / 10000;
        
        // after publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion

        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 1,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
        }

        #endregion
        
        #region check seller NFT

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(9);
        }

        #endregion

        #region check buyer NFT

        {
            var nftBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User2Address
            });
            nftBalance.Output.Balance.ShouldBe(1);
        }

        #endregion

        #region check service fee

        {
            // check buyer ELF balance
            var user1ElfBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User1Address
            });
            user1ElfBalance.Output.Balance.ShouldBe(InitializeElfAmount + sellPrice.Amount - serviceFee);
        }

        #endregion
    }
    
    [Fact]
    public async void MakeOffer_Case41_commonUser_moreCount_notDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice < sellPrice < offerPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(10_0000_0000);
        var serviceFee = sellPrice.Amount * ServiceFeeRate / 10000;
        
        // after publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));

        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion
        
        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // try
            // {
                // user2 make offer to user1
                await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
                {
                    Symbol = NftSymbol,
                    OfferTo = User1Address,
                    Quantity = 1,
                    Price = offerPrice,
                    ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
                });
                
            //     // never run this line
            //     true.ShouldBe(false);
            // }
            // catch (ShouldAssertException e)
            // {
            //     throw;
            // }
            // catch (Exception e)
            // { 
            //     e.ShouldNotBeNull();
            // }

        }

        #endregion
        
        #region check seller NFT

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(9);
        }

        #endregion

    }
    
    [Fact]
    public async void MakeOffer_Case42_commonUser_afterExpireTime_notDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice < sellPrice < offerPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(10_0000_0000);
        var serviceFee = sellPrice.Amount * ServiceFeeRate / 10000;
        
        // after publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));

        // not list

        // buy 100 NFTs
        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 100,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
             
        }

        #endregion
        
        // 100 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBeGreaterThan(0);
            offerList.Value[0].To.ShouldBe(User1Address);
            offerList.Value[0].From.ShouldBe(User2Address);
            offerList.Value[0].Quantity.ShouldBe(100);
        }

        #endregion

    }
    
    
    [Fact]
    public async void MakeOffer_Case43_commonUser_afterExpireTime_partDeal()
    {
        await InitializeForestContract();
        await PrepareNftData();
        
        // whitePrice < sellPrice < offerPrice
        var sellPrice = Elf(5_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(10_0000_0000);
        var serviceFee = sellPrice.Amount * ServiceFeeRate / 10000;
        
        // after publicTime
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5));
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));

        // list 5 NFTs
        #region ListWithFixedPrice

        {
            await Seller1ForestContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput()
            {
                Symbol = NftSymbol,
                Quantity = 5,
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
                                Value =
                                {
                                    // User2Address, 
                                    User3Address
                                },
                            }
                        },
                        // other WhitelistInfo here
                        // new WhitelistInfo() {}
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });
        }

        #endregion

        
        // buy 100 NFTs
        #region common user buy

        {
            // check buyer ELF balance
            var elfBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User2Address
            });
            elfBalance.Output.Balance.ShouldBe(InitializeElfAmount);

            // check seller ELF balance
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(10);

            // user2 make offer to user1
            await BuyerForestContractStub.MakeOffer.SendAsync(new MakeOfferInput()
            {
                Symbol = NftSymbol,
                OfferTo = User1Address,
                Quantity = 100,
                Price = offerPrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            });
             
        }

        #endregion
        
        #region check seller NFT

        {
            var nftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User1Address
            });
            nftBalance.Output.Balance.ShouldBe(5);
        }

        #endregion
        
        #region check buyer NFT

        {
            var nftBalance = await User2TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = NftSymbol,
                Owner = User2Address
            });
            nftBalance.Output.Balance.ShouldBe(5);
        }

        #endregion

        #region check service fee

        {
            // check buyer ELF balance
            var user1ElfBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = User1Address
            });
            user1ElfBalance.Output.Balance.ShouldBe(InitializeElfAmount + sellPrice.Amount * 5 - serviceFee * 5);
        }

        #endregion
        
        // 95 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBeGreaterThan(0);
            offerList.Value[0].To.ShouldBe(User1Address);
            offerList.Value[0].From.ShouldBe(User2Address);
            offerList.Value[0].Quantity.ShouldBe(95);
        }

        #endregion

    }
    
    
    
}