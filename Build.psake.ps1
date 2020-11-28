$psake.use_exit_on_error = $true

#########################################
# to build a new version
# 1. git tag 1.0.x
# 2. build package
#########################################

properties {
    $version = git describe --abbrev=0 --tags
}

Task default -depends Build

Task Build {
    Write-Output "Building Version $version"

    Exec {
        dotnet build --configuration Release /p:Version=$version
    }
}

Task Test { 
    dotnet test --configuration Release
}

Task Package -depends Test {
    nuget pack .\EventSourceProxy.nuspec -version $version
}

Task Push {
    cmd /c "for %p in (*.nupkg) do nuget push %p -source https://www.nuget.org/api/v2/package"
}