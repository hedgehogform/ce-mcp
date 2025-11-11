using System;
using CESDK.Classes;

namespace Tools
{
    public static class AddressTool
    {
        public static string GetAddressSafe(string addressString, bool local = false)
        {
            if (string.IsNullOrWhiteSpace(addressString))
                throw new ArgumentException("Address string is required.", nameof(addressString));

            var address = AddressResolver.GetAddressSafe(addressString, local);
            return address.HasValue ? $"0x{address.Value:X}" : "0";
        }
    }
}