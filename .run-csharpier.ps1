param([string]$file)

# handle normal csharpier command
Write-Host "Running csharpier on $file"
dotnet csharpier $file
