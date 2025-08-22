# GenerateGrpc.ps1
# PowerShell script to generate C# + gRPC files from cheatengine.proto

# Paths
$ProtoFile = ".\cheatengine.proto"
$OutputDir = ".\Generated"

# Path to grpc_csharp_plugin.exe (from NuGet or your preferred location)
$GrpcPlugin = "$env:USERPROFILE\.nuget\packages\grpc.tools\2.72.0\tools\windows_x64\grpc_csharp_plugin.exe"

# Make sure output folder exists
if (-Not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Run protoc
protoc --csharp_out=$OutputDir `
    --grpc_out=$OutputDir `
    --plugin=protoc-gen-grpc=$GrpcPlugin `
    $ProtoFile

Write-Host "gRPC C# files generated in $OutputDir"
