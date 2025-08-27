using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CeMCP.Models;

namespace CESDK
{
    public class MemScanTool
    {
        private readonly MemScan memScan = new MemScan();

        public MemScanResponse Scan(MemScanScanRequest request)
        {
            try
            {

                // Convert MemScanScanRequest to ScanParameters
                ScanParameters parameters = new ScanParameters()
                {
                    Value = request.Input1,
                    Value2 = request.Input2,
                    ScanOption = request.ScanOption,
                    VarType = request.VarType,
                    RoundingType = request.RoundingType,
                    StartAddress = request.StartAddress,
                    StopAddress = request.StopAddress,
                    ProtectionFlags = request.ProtectionFlags,
                    AlignmentType = request.AlignmentType,
                    AlignmentValue = request.AlignmentParam,
                    isHexadecimalInput = request.IsHexadecimalInput,
                    isUTF16Scan = request.IsUnicodeScan,
                    isCaseSensitive = request.IsCaseSensitive,
                    isPercentageScan = request.IsPercentageScan
                };

                // Start the scan
                memScan.Scan(parameters);

                // CRITICAL: Wait for the scan to complete
                memScan.WaitTillDone();

                // Get the results after scan completion
                FoundList foundList;
                int totalResults;
                
                try
                {
                    foundList = memScan.GetFoundList();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to get FoundList: {ex.Message}", ex);
                }
                
                try
                {
                    totalResults = foundList.Count;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to get FoundList Count: {ex.Message}", ex);
                }

                
                if (totalResults == 0)
                {
                    return new MemScanResponse
                    {
                        Success = true,
                        Results = null
                    };
                }
                
                var maxResults = Math.Min(totalResults, 1000);
                var results = new ResultList
                {
                    TotalCount = totalResults
                };

                // Add up to 1000 results
                for (int i = 0; i < maxResults; i++)
                {
                    results.Add(foundList.GetAddress(i), foundList.GetValue(i));
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
                memScan.Reset();
                
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
    }
}
