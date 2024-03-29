using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Standards.ACS12;
using AElf.Standards.ACS2;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Forest.Contracts.Inscription;

public partial class InscriptionContract
{
    public  ResourceInfo GetResourceInfo(Transaction txn)
    {
        switch (txn.MethodName)
        {
            case nameof(Inscribe):
            {
                var args = InscribedInput.Parser.ParseFrom(txn.Params);
                var resourceInfo = new ResourceInfo
                {
                    ReadPaths =
                    {
                        GetPath(nameof(InscriptionContractState.InscribedLimit), args.Tick.ToUpper()),
                        GetPath(nameof(InscriptionContractState.DistributorHashList), args.Tick.ToUpper())
                    }
                };
                AddDistributorAndBalancePath(resourceInfo, txn.From, args);
                // add fee path
                AddPathForTransactionFee(resourceInfo, txn.From.ToString(), txn.MethodName);
                AddPathForTransactionFeeFreeAllowance(resourceInfo, txn.From);
                AddPathForDelegatees(resourceInfo, txn.From, txn.To, txn.MethodName);

                return resourceInfo;
            }
            default:
                return new ResourceInfo { NonParallelizable = true };
        }
    }

    private void AddDistributorAndBalancePath(ResourceInfo resourceInfo, Address from, InscribedInput input)
    {
        var tick = input.Tick.ToUpper();
        var symbol = GetNftSymbol(tick);
        var distributors = State.DistributorHashList[tick];
        var selectIndex = (int)((Math.Abs(from.ToByteArray().ToInt64(true)) % distributors.Values.Count));
        var distributor = distributors.Values[selectIndex];
        var path = GetPath(nameof(State.DistributorBalance), tick, distributor.ToString());
        if (resourceInfo.WritePaths.Contains(path)) return;
        resourceInfo.WritePaths.Add(path);
        AddBalancePath(resourceInfo, distributor, symbol);
    }

    private void AddBalancePath(ResourceInfo resourceInfo, Hash distributor, string symbol)
    {
        var distributorAddress = Context.ConvertVirtualAddressToContractAddress(distributor);
        var path = GetPath(State.TokenContract.Value, "Balances", distributorAddress.ToString(), symbol);
        if (resourceInfo.WritePaths.Contains(path)) return;
        resourceInfo.WritePaths.Add(path);
    }

    private void AddPathForDelegatees(ResourceInfo resourceInfo, Address from, Address to, string methodName)
    {
        var delegateeList = new List<Address>();
        //get and add first-level delegatee list
        delegateeList.AddRange(GetDelegateeList(from, to, methodName));
        if (delegateeList.Count <= 0) return;
        var secondDelegateeList = new List<Address>();
        //get and add second-level delegatee list
        foreach (var delegateeAddress in delegateeList)
        {
            //delegatee of the first-level delegate is delegator of the second-level delegate
            secondDelegateeList.AddRange(GetDelegateeList(delegateeAddress, to, methodName));
        }

        delegateeList.AddRange(secondDelegateeList);
        foreach (var delegatee in delegateeList.Distinct())
        {
            AddPathForTransactionFee(resourceInfo, delegatee.ToString(), methodName);
            AddPathForTransactionFeeFreeAllowance(resourceInfo, delegatee);
        }
    }

    private void AddPathForTransactionFee(ResourceInfo resourceInfo, string from, string methodName)
    {
        var symbols = GetTransactionFeeSymbols(methodName);
        var primaryTokenSymbol = State.TokenContract.GetPrimaryTokenSymbol.Call(new Empty()).Value;
        if (!symbols.Contains(primaryTokenSymbol))
            symbols.Add(primaryTokenSymbol);
        var paths = symbols.Select(symbol => GetPath(State.TokenContract.Value, "Balances", from, symbol));
        foreach (var path in paths)
        {
            if (resourceInfo.WritePaths.Contains(path)) continue;
            resourceInfo.WritePaths.Add(path);
        }
    }

    private void AddPathForTransactionFeeFreeAllowance(ResourceInfo resourceInfo, Address from)
    {
        var getTransactionFeeFreeAllowancesConfigOutput =
            State.TokenContract.GetTransactionFeeFreeAllowancesConfig.Call(new Empty());
        if (getTransactionFeeFreeAllowancesConfigOutput != null)
        {
            foreach (var symbol in getTransactionFeeFreeAllowancesConfigOutput.Value.Select(config => config.Symbol))
            {
                resourceInfo.WritePaths.Add(GetPath(State.TokenContract.Value, "TransactionFeeFreeAllowances",
                    from.ToString(), symbol));
                resourceInfo.WritePaths.Add(GetPath(State.TokenContract.Value,
                    "TransactionFeeFreeAllowancesLastRefreshTimes", from.ToString(), symbol));

                var path = GetPath(State.TokenContract.Value, "TransactionFeeFreeAllowancesConfigMap", symbol);
                if (!resourceInfo.ReadPaths.Contains(path))
                {
                    resourceInfo.ReadPaths.Add(path);
                }
            }
        }
    }

    private ScopedStatePath GetPath(Address address, params string[] parts)
    {
        return new ScopedStatePath
        {
            Address = address,
            Path = new StatePath
            {
                Parts =
                {
                    parts
                }
            }
        };
    }

    private ScopedStatePath GetPath(params string[] parts)
    {
        return GetPath(Context.Self, parts);
    }

    private List<string> GetTransactionFeeSymbols(string methodName)
    {
        var actualFee = GetActualFee(methodName);
        var symbols = new List<string>();
        if (actualFee.Fees != null)
        {
            symbols = actualFee.Fees.Select(fee => fee.Symbol).Distinct().ToList();
        }

        if (!actualFee.IsSizeFeeFree)
        {
            var sizeFeeSymbols = GetSizeFeeSymbols().SymbolsToPayTxSizeFee;

            foreach (var sizeFee in sizeFeeSymbols)
            {
                if (!symbols.Contains(sizeFee.TokenSymbol))
                    symbols.Add(sizeFee.TokenSymbol);
            }
        }

        return symbols;
    }


    private UserContractMethodFees GetActualFee(string methodName)
    {
        var UserContractMethodFeeKey = "UserContractMethodFee";
        //configuration_key:UserContractMethod_contractAddress_methodName
        var spec = State.ConfigurationContract.GetConfiguration.Call(new StringValue
        {
            Value = $"{UserContractMethodFeeKey}_{Context.Self}_{methodName}"
        });
        var fee = new UserContractMethodFees();
        if (!spec.Value.IsNullOrEmpty())
        {
            fee.MergeFrom(spec.Value);
            return fee;
        }

        //If special key is null,get the normal fee set by the configuration contract.
        //configuration_key:UserContractMethod
        var value = State.ConfigurationContract.GetConfiguration.Call(new StringValue
        {
            Value = UserContractMethodFeeKey
        });
        if (value.Value.IsNullOrEmpty())
        {
            return new UserContractMethodFees();
        }

        fee.MergeFrom(value.Value);
        return fee;
    }

    private SymbolListToPayTxSizeFee GetSizeFeeSymbols()
    {
        var symbolListToPayTxSizeFee = State.TokenContract.GetSymbolsToPayTxSizeFee.Call(new Empty());
        return symbolListToPayTxSizeFee;
    }


    private List<Address> GetDelegateeList(Address delegator, Address to, string methodName)
    {
        var allDelegatees = State.TokenContract.GetTransactionFeeDelegateeList.Call(
            new GetTransactionFeeDelegateeListInput
            {
                DelegatorAddress = delegator,
                ContractAddress = to,
                MethodName = methodName
            }).DelegateeAddresses;

        if (allDelegatees == null || allDelegatees.Count == 0)
        {
            allDelegatees = State.TokenContract.GetTransactionFeeDelegatees.Call(new GetTransactionFeeDelegateesInput
            {
                DelegatorAddress = delegator
            }).DelegateeAddresses;
        }

        return allDelegatees.ToList();
    }
}