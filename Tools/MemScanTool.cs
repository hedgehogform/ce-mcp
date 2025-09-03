using System;
using System.Linq;
using CeMCP.Models;
using CESDK.Classes;

namespace CeMCP.Tools
{
    public class MemScanTool
    {
        private static readonly MemScan memoryScanner = new();

        public MemScanResponse Scan(MemScanScanRequest request)
        {
            try
            {
                // Map request to new ScanParameters
                var parameters = new ScanParameters
                {
                    ScanOption = MapScanOption(request.ScanOption.ToString()),
                    VarType = MapVarType(request.VarType.ToString()),
                    RoundingType = MapRoundingType(request.RoundingType.ToString()),
                    Input1 = request.Input1,
                    Input2 = request.Input2,
                    StartAddress = request.StartAddress,
                    StopAddress = request.StopAddress,
                    ProtectionFlags = request.ProtectionFlags ?? "+W-C",
                    AlignmentType = MapAlignmentType(request.AlignmentType.ToString()),
                    AlignmentParam = request.AlignmentParam ?? "4",
                    IsHexadecimalInput = request.IsHexadecimalInput,
                    IsUnicodeScan = request.IsUnicodeScan,
                    IsCaseSensitive = request.IsCaseSensitive
                };

                // Perform the scan
                var foundList = memoryScanner.GetAttachedFoundList();
                if (foundList == null || foundList.Count == 0)
                {
                    memoryScanner.FirstScan(parameters);
                }
                else
                {
                    memoryScanner.NextScan(parameters);
                }

                memoryScanner.WaitTillDone();

                foundList = memoryScanner.GetAttachedFoundList();
                if (foundList == null || foundList.Count == 0)
                {
                    // Even though it didn't find any results, the scan was successful
                    return new MemScanResponse
                    {
                        Success = true,
                        Results = null
                    };
                }

                var maxResults = Math.Min(foundList.Count, 1000);
                var results = new ResultList
                {
                    TotalCount = foundList.Count
                };

                for (int i = 0; i < maxResults; i++)
                {
                    var address = foundList.GetAddress(i);
                    var value = foundList.GetValue(i); // Assuming FoundList exposes GetValue
                    results.Add($"0x{address:X}", value);
                }

                return new MemScanResponse
                {
                    Success = true,
                    Results = results
                };
            }
            catch (Exception ex)
            {
                return new MemScanResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Results = new ResultList()
                };
            }
        }

        public MemScanResponse ResetScan()
        {
            try
            {
                memoryScanner.FirstScan(new ScanParameters()); // Reset via new scan object
                return new MemScanResponse
                {
                    Success = true,
                    Results = null
                };
            }
            catch (Exception ex)
            {
                return new MemScanResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Results = null
                };
            }
        }

        private static ScanOption MapScanOption(string scanOption)
        {
            return scanOption?.ToLower() switch
            {
                "sounknownvalue" => ScanOption.soUnknownValue,
                "soexactvalue" => ScanOption.soExactValue,
                "sovaluebetween" => ScanOption.soValueBetween,
                "sobiggerthan" => ScanOption.soBiggerThan,
                "sosmallerthan" => ScanOption.soSmallerThan,
                "sochanged" => ScanOption.soChanged,
                "sounchanged" => ScanOption.soUnchanged,
                "soincreased" => ScanOption.soIncreasedValue,
                "sodecreased" => ScanOption.soDecreasedValue,
                _ => ScanOption.soExactValue
            };
        }

        private static VariableType? MapVarType(string varType)
        {
            return varType?.ToLower() switch
            {
                "vtbyte" => VariableType.vtByte,
                "vtword" => VariableType.vtWord,
                "vtdword" => VariableType.vtDword,
                "vtqword" => VariableType.vtQword,
                "vtsingle" => VariableType.vtSingle,
                "vtdouble" => VariableType.vtDouble,
                "vtstring" => VariableType.vtString,
                "vtbytearray" => VariableType.vtByteArray,
                "vtall" => VariableType.vtAll,
                _ => VariableType.vtDword
            };
        }

        private static RoundingType MapRoundingType(string roundingType)
        {
            return roundingType?.ToLower() switch
            {
                "rtrounded" => RoundingType.rtRounded,
                "rttruncated" => RoundingType.rtTruncated,
                "rtextremerounded" => RoundingType.rtExtremerounded,
                _ => RoundingType.rtExtremerounded
            };
        }

        private static AlignmentType MapAlignmentType(string alignmentType)
        {
            return alignmentType?.ToLower() switch
            {
                "fsmnotaligned" => AlignmentType.fsmNotAligned,
                "fsmaligned" => AlignmentType.fsmAligned,
                "fsmlastdigits" => AlignmentType.fsmLastDigits,
                _ => AlignmentType.fsmAligned
            };
        }
    }
}
