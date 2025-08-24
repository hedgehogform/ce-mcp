using System;
using CESDK;
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

                // Use the AOB SDK wrapper instead of raw Lua
                var aob = new AOB();
                var addresses = aob.Scan(
                    request.AOBString,
                    request.ProtectionFlags,
                    request.AlignmentType,
                    request.AlignmentParam
                );

                return new AobScanResponse
                {
                    Success = true,
                    Addresses = addresses.ToArray()
                };
            }
            catch (Exception ex)
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