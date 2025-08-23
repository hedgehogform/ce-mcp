using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CeMCP.Models;

namespace CESDK
{
    class MemScanTool
    {
        private readonly MemScan memScan = new();

        public void StartScan(MemScanScanRequest request)
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
        }

        public void WaitForScan()
        {
            memScan.WaitTillDone();
        }

        public FoundList GetFoundList()
        {
            return new FoundList(memScan);
        }
    }
}
