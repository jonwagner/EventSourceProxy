$psake.use_exit_on_error = $true

#########################################
# to build a new version
# 1. git tag 1.0.x
# 2. build package
#########################################

properties {
    $baseDir = $psake.build_script_dir

    $version = git describe --abbrev=0 --tags
    $changeset = (git log -1 $version --pretty=format:%H)
	$assemblyversion = $version.Split('-', 2)[0]

    $outputDir = "$baseDir\Build\Output"

    $framework = "$env:systemroot\Microsoft.NET\Framework\v4.0.30319\"
    $msbuild = $framework + "msbuild.exe"
    $configuration = "Release"
    $nuget = "$baseDir\.nuget\nuget.exe"
    $nunit = Get-ChildItem "$baseDir\packages" -Recurse -Include nunit-console.exe
}

Task default -depends Build

function Replace-Version {
    param (
        [string] $Path
    )

    (Get-Content $Path) |
		% { $_ -replace "\[assembly: AssemblyFileVersion\(`"(\d+\.?)*`"\)\]","[assembly: AssemblyFileVersion(`"$assemblyversion`")]" } |
		Set-Content $Path
}

function ReplaceVersions {
    Get-ChildItem $baseDir -Include AssemblyInfo.cs -Recurse |% { Replace-Version $_.FullName }
}

function RestoreVersions {
    Get-ChildItem $baseDir -Include AssemblyInfo.cs -Recurse |% {
        git checkout $_.FullName
    }
}

function Wipe-Folder {
    param (
        [string] $Path
    )

    if (Test-Path $Path) { Remove-Item $Path -Recurse }
    New-Item -Path $Path -ItemType Directory | Out-Null
}

Task Build {
    ReplaceVersions

    try {
        # build the NET45 binaries
        Exec {
            Invoke-Expression "$msbuild $baseDir\EventSourceProxy.sln '/p:Configuration=$configuration' '/t:Clean;Build'"
        }
    }
    finally {
        RestoreVersions
    }
}

Task Test -depends Build { 
    Exec {
        Invoke-Expression "$nunit $baseDir\EventSourceProxy.Tests\bin\$configuration\EventSourceProxy.Tests.dll"
    }
}

Task Package -depends Test {
    Wipe-Folder $outputDir
 
    # package nuget
    Exec {
        Invoke-Expression "$nuget pack $baseDir\EventSourceProxy.nuspec -OutputDirectory $outputDir -Version $version -NoPackageAnalysis"
    }
}
