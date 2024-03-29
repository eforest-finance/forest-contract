using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Forest.Helpers;
using Forest.Whitelist;

namespace Forest.Managers;

internal class WhitelistManager
{
    private readonly CSharpSmartContractContext _context;
    private readonly MappedState<Hash, Hash> _whitelistIdMap;
    private readonly WhitelistContractContainer.WhitelistContractReferenceState _whitelistContract;

    public WhitelistManager(CSharpSmartContractContext context, MappedState<Hash, Hash> whitelistIdMap,
        WhitelistContractContainer.WhitelistContractReferenceState whitelistContract)
    {
        _context = context;
        _whitelistIdMap = whitelistIdMap;
        _whitelistContract = whitelistContract;
    }

    public void CreateWhitelist(CreateWhitelistInput input)
    {
        _whitelistContract.CreateWhitelist.Send(input);
    }

    public void AddExtraInfo(AddExtraInfoInput input)
    {
        _whitelistContract.AddExtraInfo.Send(input);
    }

    public void AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
    {
        _whitelistContract.AddAddressInfoListToWhitelist.Send(input);
    }

    public void RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
    {
        _whitelistContract.RemoveAddressInfoListFromWhitelist.Send(input);
    }

    public bool IsAddressInWhitelist(Address address, Hash whitelistId)
    {
        if (whitelistId == null)
        {
            return false;
        }

        return _whitelistContract.GetAddressFromWhitelist.Call(new GetAddressFromWhitelistInput
        {
            Address = address,
            WhitelistId = whitelistId
        }).Value;
    }

    public bool IsWhitelistAvailable(Hash whitelistId)
    {
        return whitelistId != null && _whitelistContract.GetWhitelist.Call(whitelistId).IsAvailable;
    }

    public Price GetExtraInfoByAddress(Hash whitelistId)
    {
        var tagInfo = _whitelistContract.GetExtraInfoByAddress.Call(new GetExtraInfoByAddressInput
        {
            WhitelistId = whitelistId,
            Address = _context.Sender
        });
        return WhitelistHelper.DeserializedInfo(tagInfo);
    }

    public bool GetTagInfoFromWhitelist(GetTagInfoFromWhitelistInput input)
    {
        return _whitelistContract.GetTagInfoFromWhitelist.Call(input).Value;
    }
}