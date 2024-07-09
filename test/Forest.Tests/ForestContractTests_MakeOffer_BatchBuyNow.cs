using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Forest;

public partial class ForestContractTests_MakeOffer
{
    /*private async Task<Timestamp> InitUserListInfo(int listQuantity, long inputSellPrice, int approveQuantity,
        TokenContractImplContainer.TokenContractImplStub userTokenContractStub
        , ForestContractContainer.ForestContractStub sellerForestContractStub ,Address userAddress)
    {
        var sellPrice = Elf(inputSellPrice);
        {
            await userTokenContractStub.Approve.SendAsync(new ApproveInput()
                { Spender = ForestContractAddress, Symbol = NftSymbol, Amount = approveQuantity });
            var startTime = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddSeconds(approveQuantity));
            var executionResult = await sellerForestContractStub.ListWithFixedPrice.SendAsync(
                new ListWithFixedPriceInput
                {
                    Symbol = NftSymbol,
                    Quantity = listQuantity,
                    IsWhitelistAvailable = true,
                    Price = sellPrice,
                    Duration = new ListWithFixedPriceDuration()
                    {
                        StartTime = startTime,
                        PublicTime = startTime,
                    }
                });
            var log = ListedNFTAdded.Parser
                .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTAdded))
                    .NonIndexed);
            log.Owner.ShouldBe(userAddress);
            log.Quantity.ShouldBe(listQuantity);
            log.Symbol.ShouldBe(NftSymbol);
            log.Price.Symbol.ShouldBe(ElfSymbol);
            log.Price.Amount.ShouldBe(inputSellPrice);
            log.Duration.StartTime.ShouldNotBeNull();
            log.Duration.DurationHours.ShouldBe(0);
            log.Duration.DurationMinutes.ShouldBe(263520);
            
            var listedNftInfo = (await Seller1ForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = userAddress
                })).Value.Last();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(inputSellPrice);
            listedNftInfo.Quantity.ShouldBe(listQuantity);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(0);
            listedNftInfo.Duration.DurationMinutes.ShouldBe(263520);
            return startTime;
        }
    }

    private async Task QueryLastByStartAscListInfo(ForestContractContainer.ForestContractStub sellerForestContractStub
        , int intpuListQuantity, long inputSellPrice, Address userAddress)
    {
        {
            var listedNftInfo = (await sellerForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = userAddress
                })).Value.Last();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(inputSellPrice);
            listedNftInfo.Quantity.ShouldBe(intpuListQuantity);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(0);
            listedNftInfo.Duration.DurationMinutes.ShouldBe(263520);
        }
    }

    private async Task QueryFirstByStartAscListInfo(ForestContractContainer.ForestContractStub sellerForestContractStub
        , int intpuListQuantity, long inputSellPrice, Address userAddress)
    {
        {
            var listedNftInfo = (await sellerForestContractStub.GetListedNFTInfoList.CallAsync(
                new GetListedNFTInfoListInput
                {
                    Symbol = NftSymbol,
                    Owner = userAddress
                })).Value.First();
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(inputSellPrice);
            listedNftInfo.Quantity.ShouldBe(intpuListQuantity);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(0);
            listedNftInfo.Duration.DurationMinutes.ShouldBe(263520);
        }
    }

    [Fact]
    public async void BatchBuyNow_Buy_From_OneUser_Listing_Records_Success()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 2;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 3;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 1;
        var user3InputSellPrice1 = 2;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 3;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime2,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity2,
                Price = new Price()
                {
                    Amount = user1InputSellPrice2,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var executionResult = await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);
            var log = BatchBuyNowResult.Parser.ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(BatchBuyNowResult))
                .NonIndexed);
            log.Symbol.ShouldBe("TESTNFT-1");
            log.AllSuccessFlag.ShouldBe(true);
            log.FailPriceList.ShouldBe(new FailPriceList());
        }

        #endregion

        // user1 nft number from 10 to 7
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(7);
        
        // user 2 nft number from 0 to 3
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(3);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    
    [Fact]
    public async void BatchBuyNow_Buy_From_OneUser_Listing_Price_Not_Exist_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 2;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 3;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 1;
        var user3InputSellPrice1 = 2;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 3;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1*3,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime2,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity2,
                Price = new Price()
                {
                    Amount = user1InputSellPrice2*3,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("NormalPrice does not exist.");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    
    [Fact]
    public async void BatchBuyNow_Buy_From_OneUser_Sender_Balance_Is_Zero_Success()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 5;
        var user1InputSellPrice1 = 2;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        
        #endregion basic end

        #region user1 transfer to other
        await UserTokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User3Address,
            Symbol = NftSymbol,
            Amount = 10
        });
        #endregion

        
        #region BatchBuyNow user2 buy from user1 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
           
            fixPriceList.Add(priceList1);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var executionResult = await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);
            var log = BatchBuyNowResult.Parser.ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(BatchBuyNowResult))
                .NonIndexed);
            log.Symbol.ShouldBe("TESTNFT-1");
            log.AllSuccessFlag.ShouldBe(false);
            log.FailPriceList.ShouldNotBeNull();
            log.FailPriceList.ShouldNotBeNull();
            log.FailPriceList.Value.First().Quantity.ShouldBe(user1InputListQuantity1);
            log.FailPriceList.Value.First().Price.Amount.ShouldBe(user1InputSellPrice1);
            log.FailPriceList.Value.First().Price.Symbol.ShouldBe("ELF");
        }

        #endregion

        // user1 nft number from 10 to 0
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(0);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    [Fact]
    public async void BatchBuyNow_Buy_From_OneUser_Listing_Records_Part_Success()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 10;
        var user1InputSellPrice1 = 10;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        #endregion basic end
        
        #region user1 transfer to other
        await UserTokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User3Address,
            Symbol = NftSymbol,
            Amount = 8
        });
        #endregion

        #region BatchBuyNow user2 buy from user1 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = 3,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            
            fixPriceList.Add(priceList1);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var executionResult = await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);
            var log = BatchBuyNowResult.Parser.ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(BatchBuyNowResult))
                .NonIndexed);
            log.Symbol.ShouldBe("TESTNFT-1");
            log.AllSuccessFlag.ShouldBe(false);
            log.FailPriceList.ShouldNotBeNull();
            log.FailPriceList.Value.First().Quantity.ShouldBe(1L);
            log.FailPriceList.Value.First().Price.Amount.ShouldBe(user1InputSellPrice1);
            log.FailPriceList.Value.First().Price.Symbol.ShouldBe("ELF");
            
        }

        #endregion

        // user1 nft number from 10 to 0
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(0);
        
        // user 2 nft number from 0 to 2
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(2);
        
        // user 3 nft number from 10 to 18
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(18);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    
    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_Success()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 2;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 3;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 2;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 3;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var executionResult = await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);
            var log = BatchBuyNowResult.Parser.ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(BatchBuyNowResult))
                .NonIndexed);
            log.Symbol.ShouldBe("TESTNFT-1");
            log.AllSuccessFlag.ShouldBe(true);
            log.FailPriceList.ShouldBe(new FailPriceList());
            
            
        }

        #endregion
        
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);

        // user1 nft number from 10 to 9
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(9);
        // user3 nft number from 10 to 7
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(7);
        
        // user 2 nft number from 0 to 4
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(4);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_Part_Success()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 5;
        var user1InputSellPrice1 = 10;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 5;
        var user3InputSellPrice1 = 20;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        
        #endregion basic end

        #region user1 transfer to other
        await UserTokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User4Address,
            Symbol = NftSymbol,
            Amount = 7
        });
        #endregion
        
        #region user3 transfer to other
        await User3TokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User4Address,
            Symbol = NftSymbol,
            Amount = 8
        });
        #endregion
        
        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = 5,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = 3,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var executionResult = await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);
            var log = BatchBuyNowResult.Parser.ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(BatchBuyNowResult))
                .NonIndexed);
            log.Symbol.ShouldBe("TESTNFT-1");
            log.AllSuccessFlag.ShouldBe(false);
            log.FailPriceList.ShouldNotBeNull();
            log.FailPriceList.Value.Count.ShouldBe(2);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).First().Quantity.ShouldBe(2L);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).First().Price.Amount.ShouldBe(user1InputSellPrice1);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).First().Price.Symbol.ShouldBe("ELF");
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).Skip(1).First().Quantity.ShouldBe(1L);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).Skip(1).First().Price.Amount.ShouldBe(user3InputSellPrice1);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).Skip(1).First().Price.Symbol.ShouldBe("ELF");
            
        }

        #endregion
        
        // user1 nft number from 10 to 0
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(0);
        // user3 nft number from 10 to 0
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(0);
        
        // user 2 nft number from 0 to 3
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(5);
        
        // user 4 nft number from 0 to 15
        var user4NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User4Address
        });
        user4NftBalance.Output.Balance.ShouldBe(15);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_User1Part_Success()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 5;
        var user1InputSellPrice1 = 10;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 5;
        var user3InputSellPrice1 = 20;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        
        #endregion basic end

        #region user1 transfer to other
        await UserTokenContractStub.Transfer.SendAsync(new TransferInput()
        {
            To = User4Address,
            Symbol = NftSymbol,
            Amount = 7
        });
        #endregion

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = 5,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = 5,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var executionResult = await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);
            var log = BatchBuyNowResult.Parser.ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name == nameof(BatchBuyNowResult))
                .NonIndexed);
            log.Symbol.ShouldBe("TESTNFT-1");
            log.AllSuccessFlag.ShouldBe(false);
            log.FailPriceList.ShouldNotBeNull();
            log.FailPriceList.Value.Count.ShouldBe(1);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).First().Quantity.ShouldBe(2L);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).First().Price.Amount.ShouldBe(user1InputSellPrice1);
            log.FailPriceList.Value.OrderBy(item=>item.Price.Amount).First().Price.Symbol.ShouldBe("ELF");

        }

        #endregion
        
        // user1 nft number from 10 to 0
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(0);
        // user3 nft number from 10 to 5
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(5);
        
        // user 2 nft number from 0 to 8
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(8);
        
        // user 4 nft number from 0 to 7
        var user4NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User4Address
        });
        user4NftBalance.Output.Balance.ShouldBe(7);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    
    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_Buyer_Balance_Not_Enough_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 10000_0000_0000;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 20000_0000_0000;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 30000_0000_0000;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 40000_0000_0000;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("Insufficient funds");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_Nft_Not_Exist_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 100;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 200;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 300;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 400;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol+"abc";
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("Invalid symbol data");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }

    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_OfferTo_Not_Exist_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 100;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 200;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 300;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 400;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = null,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("Invalid param OfferTo.");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
     [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_Price_Symbol_Not_Exist_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 100;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 200;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 300;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 400;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELFA"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("Insufficient funds");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
     [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_Symbol_Is_Null_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 100;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 200;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 300;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 400;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            //batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("Invalid symbol data");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }
    
    [Fact]
    public async void BatchBuyNow_Buy_From_TwoUser_Listing_Records_FixPriceList_Is_Null_Fail()
    {
        await InitializeForestContract();
        await PrepareNftData();
        #region basic begin
        //seller user1 add listing
        var user1ApproveQuantity = 0;
        var user1InputListQuantity1 = 1;
        var user1InputSellPrice1 = 100;
        user1ApproveQuantity += user1InputListQuantity1;
        var startTime1 = await InitUserListInfo(user1InputListQuantity1, user1InputSellPrice1, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        var user1InputListQuantity2 = 2;
        var user1InputSellPrice2 = 200;
        user1ApproveQuantity += user1InputListQuantity2;
        var startTime2 = await InitUserListInfo(user1InputListQuantity2, user1InputSellPrice2, user1ApproveQuantity
            , UserTokenContractStub, Seller1ForestContractStub, User1Address);
        await QueryLastByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity2, user1InputSellPrice2,
            User1Address);
        await QueryFirstByStartAscListInfo(Seller1ForestContractStub, user1InputListQuantity1, user1InputSellPrice1,
            User1Address);

        //seller user3 add listing
        var user3ApproveQuantity = 0;
        var user3InputListQuantity1 = 3;
        var user3InputSellPrice1 = 300;
        user3ApproveQuantity += user3InputListQuantity1;
        var startTime3 = await InitUserListInfo(user3InputListQuantity1, user3InputSellPrice1, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        var user3InputListQuantity2 = 2;
        var user3InputSellPrice2 = 400;
        user3ApproveQuantity += user3InputListQuantity2;
        var startTime4 = await InitUserListInfo(user3InputListQuantity2, user3InputSellPrice2, user3ApproveQuantity
            , User3TokenContractStub, Seller3ForestContractStub, User3Address);
        await QueryLastByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity2, user3InputSellPrice2,
            User3Address);
        await QueryFirstByStartAscListInfo(Seller3ForestContractStub, user3InputListQuantity1, user3InputSellPrice1,
            User3Address);

        #endregion basic end

        #region BatchBuyNow user2 buy from user1、user3 listing records
        
        {
            //modify BlockTime
            var BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddMinutes(5));

            var batchBuyNowInput = new BatchBuyNowInput();
            batchBuyNowInput.Symbol = NftSymbol;
            var fixPriceList = new RepeatedField<FixPrice>(); 
            var priceList1 = new FixPrice()
            {
                StartTime = startTime1,
                OfferTo = User1Address,
                Quantity = user1InputListQuantity1,
                Price = new Price()
                {
                    Amount = user1InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            var priceList2 = new FixPrice()
            {
                StartTime = startTime3,
                OfferTo = User3Address,
                Quantity = user3InputListQuantity1,
                Price = new Price()
                {
                    Amount = user3InputSellPrice1,
                    Symbol = "ELF"
                }
            };
            fixPriceList.Add(priceList1);
            fixPriceList.Add(priceList2);
            //batchBuyNowInput.FixPriceList.AddRange(fixPriceList);
            
            // user2 BatchBuyNow
            var errorMessage = "";
            try
            {
                await BuyerForestContractStub.BatchBuyNow.SendAsync(batchBuyNowInput);

            }catch (Exception e)
            {
                errorMessage = e.Message;
            }
            errorMessage.ShouldContain("Invalid input data");
        }

        #endregion

        // user1 nft number from 10 to 10
        var user1NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User1Address
        });
        user1NftBalance.Output.Balance.ShouldBe(10);
        // user3 nft number from 10 to 10
        var user3NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User3Address
        });
        user3NftBalance.Output.Balance.ShouldBe(10);
        
        // user 2 nft number from 0 to 0
        var user2NftBalance = await UserTokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Symbol = NftSymbol,
            Owner = User2Address
        });
        user2NftBalance.Output.Balance.ShouldBe(0);
        
        // 0 NFTs to offer list
        #region check offer list

        {
            // list offers just sent
            var offerList = BuyerForestContractStub.GetOfferList.SendAsync(new GetOfferListInput()
            {
                Symbol = NftSymbol,
                Address = User2Address,
            }).Result.Output;
            offerList.Value.Count.ShouldBe(0);
        }

        #endregion
    }*/
}