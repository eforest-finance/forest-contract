using System.Threading.Tasks;
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

       // await AdminForestContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);
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
    //cancel offer
    public async void SetAIServiceFee_Test()
    {
        await InitializeForestContract();
    

    }
    
}