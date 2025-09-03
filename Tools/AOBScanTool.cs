using System;
using System.Linq;
using CESDK.Classes;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class AobScanTool
    {
        public AobScanResponse AOBScan(AobScanRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AOBString))
                {
                    return new AobScanResponse
                    {
                        Success = false,
                        Error = "AOBString parameter is required"
                    };
                }

                // Perform the AOB scan
                var result = AOBScanner.Scan(request.AOBString!);

                return new AobScanResponse
                {
                    Success = true,
                    Addresses = [.. result.Select(addr => $"0x{addr:X}")]
                };
            }
            catch (AOBScanException ex)
            {
                return new AobScanResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}