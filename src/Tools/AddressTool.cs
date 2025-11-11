using System;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class AddressTool
    {
        /// <summary>
        /// Maps address resolution API endpoints
        /// </summary>
        public static void MapAddressApi(this WebApplication app)
        {
            // POST /api/address/resolve - Resolve address from string
            app.MapPost("/api/address/resolve", (AddressResolveRequest request) =>
            {
                try
                {
                    var address = GetAddressSafe(request.AddressString ?? "", request.Local ?? false);
                    return Results.Ok(new { success = true, address });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("ResolveAddress")
            .WithDescription("Resolve an address from a string expression")
            .WithOpenApi();
        }

        private static string GetAddressSafe(string addressString, bool local = false)
        {
            if (string.IsNullOrWhiteSpace(addressString))
                throw new ArgumentException("Address string is required.", nameof(addressString));

            var address = AddressResolver.GetAddressSafe(addressString, local);
            return address.HasValue ? $"0x{address.Value:X}" : "0";
        }
    }

    public record AddressResolveRequest(string? AddressString, bool? Local = false);
}