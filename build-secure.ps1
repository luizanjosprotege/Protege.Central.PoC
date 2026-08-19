<#
.SYNOPSIS
  Build de release "seguro" do Protege.Central.PoC:
  publica self-contained e ofusca as DLLs proprias (Core + App) com Obfuscar,
  para dificultar engenharia reversa (renomeia classes/metodos/campos privados,
  remove PDBs, suprime ildasm).

.NOTES
  Isso NAO torna o app inquebravel - qualquer binario .NET rodando localmente
  pode, em tese, ser instrumentado/decompilado por quem tem acesso a maquina.
  A ofuscacao eleva bastante o esforco necessario (renomeia toda a API interna),
  o que e o objetivo pratico para uma PoC.
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$appProj = Join-Path $root "src\Protege.Central.PoC.App\Protege.Central.PoC.App.csproj"
$rawDir = Join-Path $root "build\_raw"
$outDir = Join-Path $root "publish"
$obfuscar = Join-Path $root "tools\obfuscar\Obfuscar.Console.exe"
$obfuscarProject = Join-Path $root "build\obfuscar.xml"
$mapFile = Join-Path $root "build\Mapping.txt"
$obfDir = Join-Path $root "build\_obfuscated"

function Step($msg) { Write-Host ""; Write-Host "==> $msg" -ForegroundColor Cyan }
function Ok($msg) { Write-Host "  [OK] $msg" -ForegroundColor Green }

Step "Limpando builds anteriores..."
Remove-Item $rawDir, $outDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path (Join-Path $root "build") | Out-Null

Step "Publicando (self-contained, $Runtime)..."
dotnet publish $appProj -c $Configuration -r $Runtime --self-contained true -o $rawDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }
Ok "Publicado em $rawDir"

Step "Gerando projeto Obfuscar..."
$xml = @"
<Obfuscator>
  <Var name="InPath" value="$rawDir" />
  <Var name="OutPath" value="$obfDir" />
  <Var name="LogFilePath" value="$mapFile" />
  <Var name="RenamePdb" value="false" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="KeepPublicApi" value="false" />
  <Var name="OptimizeMethods" value="true" />
  <Var name="SuppressIldasm" value="true" />
  <Var name="UseUnicodeNames" value="false" />
  <Var name="Verbose" value="false" />
  <Module file="`$(InPath)\Protege.Central.PoC.Core.dll" />
  <Module file="`$(InPath)\Protege.Central.PoC.dll" />
</Obfuscator>
"@
Set-Content -Path $obfuscarProject -Value $xml -Encoding utf8
Ok "obfuscar.xml gerado"

Step "Ofuscando Protege.Central.PoC.Core.dll e Protege.Central.PoC.dll..."
& $obfuscar $obfuscarProject
if ($LASTEXITCODE -ne 0) { throw "Obfuscar falhou." }
Ok "Assemblies ofuscados (nomes internos de classes/metodos/campos renomeados)"

Step "Substituindo DLLs originais pelas versoes ofuscadas..."
Copy-Item (Join-Path $obfDir "Protege.Central.PoC.Core.dll") $rawDir -Force
Copy-Item (Join-Path $obfDir "Protege.Central.PoC.dll") $rawDir -Force
Ok "DLLs ofuscadas aplicadas ao artefato publicado"

Step "Limpando arquivos de debug/mapa do artefato final..."
Get-ChildItem $rawDir -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force
Ok "PDBs removidos do artefato publicado"

Step "Movendo artefato final para publish\..."
Move-Item $rawDir $outDir

$exe = Join-Path $outDir "Protege.Central.PoC.exe"
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host " Build seguro concluido" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  Executavel : $exe"
Write-Host "  Mapa de nomes ofuscados (privado, NAO distribuir): $mapFile"
Write-Host ""
