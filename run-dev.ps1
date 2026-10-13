# Starts the API for local development with the settings from .env (copy .env.example to .env).
# The API reads them at run time; nothing is written into the code or the build.
$envFile = Join-Path $PSScriptRoot '.env'
if (Test-Path $envFile) {
    Get-Content $envFile | Where-Object { $_ -match '^\s*[^#\s][^=]*=' } | ForEach-Object {
        $name, $value = $_ -split '=', 2
        Set-Item -Path "Env:$($name.Trim())" -Value $value.Trim()
    }
}

dotnet run --project (Join-Path $PSScriptRoot 'backend/src/Vas.Api')
