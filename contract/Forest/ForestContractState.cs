using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Forest
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public partial class ForestContractState : ContractState
    {
        public SingletonState<Address> Admin { get; set; }

        public SingletonState<Address> ServiceFeeReceiver { get; set; }
        
        public SingletonState<BizConfig> BizConfig { get; set; }
        
        public Int32State ServiceFeeRate { get; set; }
        public Int64State ServiceFee { get; set; }

        public SingletonState<StringList> GlobalTokenWhiteList { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Owner -> List NFT Info List
        /// </summary>
        public MappedState<string, Address, ListedNFTInfoList> ListedNFTInfoListMap { get; set; }

        /// <summary>
        /// Project Id -> Whitelist Id
        /// </summary>
        public MappedState<Hash, Hash> WhitelistIdMap { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Offer Address List
        /// </summary>
        public MappedState<string, AddressList> OfferAddressListMap { get; set; }

        /// <summary>
        /// Symbol ->  Offer Maker address -> Offer List
        /// </summary>
        public MappedState<string, Address, OfferList> OfferListMap { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Royalty
        /// </summary>
        public MappedState<string, int> RoyaltyMap { get; set; }

        public MappedState<string, Address> RoyaltyFeeReceiverMap { get; set; }
        public MappedState<string, CertainNFTRoyaltyInfo> CertainNFTRoyaltyMap { get; set; }
        public MappedState<string, StringList> TokenWhiteListMap { get; set; }
        
        /// <summary>
        /// Address -> Token Symbol -> TotalAmount
        /// </summary>
        public MappedState<Address, string, long> OfferTotalAmountMap { get; set; }

        public SingletonState<Price> AIServiceFeeConfig { get; set; }
        public SingletonState<Address> AIServiceFeeReceiver { get; set; }
        
        public SingletonState<StringList> AIImageSizeList { get; set; }
        /// <summary>
        /// Symbol -> Address TxId ->  CreateArtInfo
        /// </summary>
        public MappedState<Address, string, CreateArtInfo> CreateArtInfoMap { get; set; }
        public SingletonState<int> MaxBatchCancelOfferCount { get; set; }
        
        public SingletonState<int> MaxBatchCancelListCount { get; set; }

    }
}