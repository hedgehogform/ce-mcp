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
                    IsCaseSensitive = request.IsCaseSensitive,
                    IsPercentageScan = request.IsPercentageScan
                };

                // Determine if this is a first scan or next scan
                var existingFoundList = memoryScanner.GetAttachedFoundList();
                if (existingFoundList == null || existingFoundList.Count <= 0)
                {
                    // Perform first scan
                    memoryScanner.FirstScan(parameters);
                }
                else
                {
                    // Perform next scan
                    memoryScanner.NextScan(parameters);
                }

                // Wait for the scan to complete
                memoryScanner.WaitTillDone();

                // Get the attached found list after scan completion
                var foundList = memoryScanner.GetAttachedFoundList();
                if (foundList == null)
                {
                    return new MemScanResponse
                    {
                        Success = true,
                        Results = new ResultList { TotalCount = 0 }
                    };
                }

                // Initialize the found list for reading results
                foundList.Initialize();

                var count = foundList.Count;
                if (count <= 0)
                {
                    return new MemScanResponse
                    {
                        Success = true,
                        Results = new ResultList { TotalCount = 0 }
                    };
                }

                // Limit results to prevent memory issues (max 1000 results)
                var maxResults = Math.Min(count, 1000);
                var results = new ResultList
                {
                    TotalCount = count
                };

                for (int i = 0; i < maxResults; i++)
                {
                    try
                    {
                        var address = foundList.GetAddress(i);
                        var value = foundList.GetValue(i);
                        results.Add(address, value);
                    }
                    catch (Exception ex)
                    {
                        // If we can't get individual results, break and return what we have
                        System.Diagnostics.Debug.WriteLine($"Error getting result {i}: {ex.Message}");
                        break;
                    }
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
                // Get current found list and deinitialize it if it exists
                var currentFoundList = memoryScanner.GetAttachedFoundList();
                if (currentFoundList != null && currentFoundList.IsInitialized)
                {
                    currentFoundList.Deinitialize();
                }

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
