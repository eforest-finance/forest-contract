using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Forest;

public class ForestContractTests_TreePoints : ForestContractTestBase
{
    private const string NftSymbol = "TESTNFT-1";
    private const string NftSymbol2 = "TESTNFT-2";
    private const string ElfSymbol = "ELF";
    private const int ServiceFeeRate = 1000; // 10%
    private const int AIServiceFee = 10000000; 
    private const string DefaultAIImageSize1024 = "1024x1024";
    private const string DefaultAIImageSize512 = "512x512";
    private const string DefaultAIImageSize256 = "256x256";

    private async Task InitializeForestContract()
    {
        await AdminForestContractStub.Initialize.SendAsync(new InitializeInput
        {
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress,
            ServiceFeeRate = ServiceFeeRate,
            WhitelistContractAddress = WhitelistContractAddress
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
    
    [Fact]
    public async void AddTreePoints_Test()
    {
        await InitializeForestContract();
        await ForestContractStub.SetTreePointsHashVerifyKey.SendAsync(new StringValue(){Value = "1a2b3c"});
        var points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(0);
        await ForestContractStub.AddTreePoints.SendAsync(new AddTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 10,
            PointsType = 0,
            OpTime = 1730454324136,
            RequestHash = "e1320b97e10afc3a37ff3df54ef811ba97b77da39972ba383b2c351bd656e4cc"
        });
        points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(10);
        var addResult = await ForestContractStub.AddTreePoints.SendWithExceptionAsync(new AddTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 10,
            PointsType = 0,
            OpTime = 1730454324136,
            RequestHash = "e1320b97e10afc3a37ff3df54ef811ba97b77da39972ba383b2c351bd656e4cc"
        });
        addResult.TransactionResult.Error.ShouldContain("Invalid param OpTime");
    }
    [Fact]
    public async void TreeLevelUpgrade_Test()
    {
        await InitializeForestContract();
        await ForestContractStub.SetTreePointsHashVerifyKey.SendAsync(new StringValue(){Value = "1a2b3c"});
        var points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(0);
        var result = await ForestContractStub.TreeLevelUpgrade.SendWithExceptionAsync(new TreeLevelUpgradeInput()
        {
            Address = DefaultAddress,
            Points = 100,
            UpgradeLevel = 2,
            OpTime = 1730462882565,
            RequestHash = "d3a274f226217fc6a18c250df41f10ae8fadc30d5e933dcdad1a75a51e1d26b7"
        });
        result.TransactionResult.Error.ShouldContain("your points is zero");
        
        await ForestContractStub.AddTreePoints.SendAsync(new AddTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 110,
            PointsType = 0,
            OpTime = 1730454324136,
            RequestHash = "5494bedf4cb1d69920b17d89fbc1d6c5f18e46476b347ff8aa7c843d7faf7183"
        });
        
        points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(110);
        
        result = await ForestContractStub.TreeLevelUpgrade.SendAsync(new TreeLevelUpgradeInput()
        {
            Address = DefaultAddress,
            Points = 100,
            UpgradeLevel = 2,
            OpTime = 1730462882565,
            RequestHash = "d3a274f226217fc6a18c250df41f10ae8fadc30d5e933dcdad1a75a51e1d26b7"
        });
        points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(10);
        
        result = await ForestContractStub.TreeLevelUpgrade.SendWithExceptionAsync(new TreeLevelUpgradeInput()
        {
            Address = DefaultAddress,
            Points = 100,
            UpgradeLevel = 2,
            OpTime = 1730462882565,
            RequestHash = "d3a274f226217fc6a18c250df41f10ae8fadc30d5e933dcdad1a75a51e1d26b7"
        });
        result.TransactionResult.Error.ShouldContain("Invalid param OpTime");
    }
    
    
     [Fact]
    public async void ClaimTreePoints_Test()
    {
        await InitializeForestContract();
        await ForestContractStub.SetTreePointsHashVerifyKey.SendAsync(new StringValue(){Value = "1a2b3c"});
        {
            //transfer balance
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                To = ForestContractAddress,
                Symbol = ElfSymbol,
                Amount = 10000000000
            });
            
            var nftBalance = await UserTokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Symbol = ElfSymbol,
                Owner = ForestContractAddress
            });
            nftBalance.Balance.ShouldBe(10000000000);
        }



        var points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(0);
        var result = await ForestContractStub.ClaimTreePoints.SendWithExceptionAsync(new ClaimTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 0,
            ActivityId = "f9235130fda7230d9084374562248961d21e18e8445071b56e0630488623202e",
            OpTime = 1730471642155,
            Reward = new TreeReward()
            {
                Symbol = "ELF",
                Amount = 1000000000
            },
            RequestHash = "41f1cd1ad97ea5bc7cf2513b805b899750b2d879cdf6fa9d8802b39e62f95093"
        });
        result.TransactionResult.Error.ShouldContain("your points is zero");
        
        await ForestContractStub.AddTreePoints.SendAsync(new AddTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 110,
            PointsType = 0,
            OpTime = 1730454324136,
            RequestHash = "5494bedf4cb1d69920b17d89fbc1d6c5f18e46476b347ff8aa7c843d7faf7183"
        });
        
        points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(110);
        
        result = await ForestContractStub.ClaimTreePoints.SendAsync(new ClaimTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 0,
            ActivityId = "f9235130fda7230d9084374562248961d21e18e8445071b56e0630488623202e",
            Reward = new TreeReward()
            {
                Symbol = "ELF",
                Amount = 1000000000
            },
            OpTime = 1730471642155,
            RequestHash = "41f1cd1ad97ea5bc7cf2513b805b899750b2d879cdf6fa9d8802b39e62f95093"
        });
        points = await ForestContractStub.GetTreePoints.CallAsync(DefaultAddress);
        points.Points.ShouldBe(110);
        
        result = await ForestContractStub.ClaimTreePoints.SendWithExceptionAsync(new ClaimTreePointsInput()
        {
            Address = DefaultAddress,
            Points = 0,
            ActivityId = "f9235130fda7230d9084374562248961d21e18e8445071b56e0630488623202e",
            Reward = new TreeReward()
            {
                Symbol = "ELF",
                Amount = 1000000000
            },
            OpTime = 1730471642155,
            RequestHash = "41f1cd1ad97ea5bc7cf2513b805b899750b2d879cdf6fa9d8802b39e62f95093"
        });
        result.TransactionResult.Error.ShouldContain("Invalid param OpTime");
    }
}