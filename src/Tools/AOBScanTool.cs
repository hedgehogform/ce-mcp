using System;
using System.Linq;
using System.Collections.Generic;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class AobScanTool
    {
        /// <summary>
        /// Maps AOB scan API endpoints
        /// </summary>
        public static void MapAobScanApi(this WebApplication app)
        {
            // POST /api/aob/scan - Scan for Array of Bytes pattern
            app.MapPost("/api/aob/scan", (AobScanRequest request) =>
            {
                try
                {
                    var addresses = AobScan(
                        request.Pattern ?? "",
                        request.ProtectionFlags,
                        request.AlignmentType,
                        request.AlignmentParam);
                    return Results.Ok(new { success = true, addresses });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("AobScan")
            .WithDescription("Scan memory for an Array of Bytes pattern")
            .WithOpenApi();
        }

        private static List<string> AobScan(
            string aobString,
            string? protectionFlags = null,
            int? alignmentType = null,
            string? alignmentParam = null)
        {
            if (string.IsNullOrWhiteSpace(aobString))
                throw new ArgumentException("AOB string is required.", nameof(aobString));

            try
            {
                var result = AOBScanner.Scan(
                    aobString,
                    protectionFlags,
                    alignmentType ?? 0,
                    alignmentParam
                );

                return [.. result.Select(addr => $"0x{addr:X}")];
            }
            catch (AOBScanException ex)
            {
                throw new InvalidOperationException($"AOB scan failed: {ex.Message}", ex);
            }
        }
    }

    public record AobScanRequest(
        string? Pattern,
        string? ProtectionFlags = null,
        int? AlignmentType = null,
        string? AlignmentParam = null);
}