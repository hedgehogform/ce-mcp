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
        private readonly MemScan memScan = new();



        public MemScanResponse Scan(MemScanScanRequest request)
        {
            // Convert MemScanScanRequest to ScanParameters
            ScanParameters parameters = new()
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

            memScan.Scan(parameters);

            var foundList = memScan.GetFoundList();
            foundList.Initialize();
            var countResults = foundList.Count;
            var results = new ResultList
            {
                TotalCount = countResults
            };

            // Only return max 1000 results by for loop.
            for (int i = 0; i < Math.Min(countResults, 1000); i++)
            {
                results.Add(foundList.GetAddress(i), foundList.GetValue(i));
            }

            return new MemScanResponse
            {
                Success = true,
                Results = results
            };
        }
    }
}
