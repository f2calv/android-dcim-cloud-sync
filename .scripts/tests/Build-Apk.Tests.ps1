#Requires -Modules @{ ModuleName = 'Pester'; RequiredVersion = '5.7.1' }

BeforeAll {
    . (Join-Path (Split-Path $PSScriptRoot -Parent) 'Build-Apk.ps1')
}

Describe 'Get-ApkVersionCode' {
    It 'Counts whole minutes since 2025-01-01 UTC' {
        $now = [DateTimeOffset]::new(2025, 1, 2, 0, 0, 59, [TimeSpan]::Zero)
        Get-ApkVersionCode -Now $now | Should -Be 1440
    }

    It 'Grows with time so later builds upgrade earlier ones' {
        $earlier = Get-ApkVersionCode -Now ([DateTimeOffset]::new(2026, 11, 7, 18, 0, 0, [TimeSpan]::Zero))
        $later = Get-ApkVersionCode -Now ([DateTimeOffset]::new(2026, 11, 7, 18, 1, 0, [TimeSpan]::Zero))
        $later | Should -BeGreaterThan $earlier
    }

    It 'Rejects a clock before the epoch' {
        { Get-ApkVersionCode -Now ([DateTimeOffset]::new(2024, 12, 31, 0, 0, 0, [TimeSpan]::Zero)) } | Should -Throw '*-VersionCode*'
    }
}

Describe 'Get-ApkDisplayVersion' {
    It 'Combines the latest release tag and the commit' {
        Mock Invoke-Git { if ($args -contains 'describe') { '1.2.3' } else { 'abc1234' } }
        Get-ApkDisplayVersion -Root $TestDrive | Should -Be '1.2.3-local.abc1234'
    }

    It 'Falls back when there is no release tag or commit' {
        Mock Invoke-Git { }
        Get-ApkDisplayVersion -Root $TestDrive | Should -Be '0.0.0-local'
    }
}

Describe 'Build-Apk' {
    BeforeEach {
        $root = Join-Path $TestDrive 'repo'
        $projectFolder = Join-Path $root 'src/DcimCloudSync'
        New-Item -ItemType Directory -Path $projectFolder -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $projectFolder 'DcimCloudSync.csproj') -Value '<Project />'
        $destination = Join-Path $TestDrive ([guid]::NewGuid().ToString('n'))
        $script:PublishDirectory = Join-Path $destination 'publish'
        $script:PublishArguments = $null
        $script:Commands = @()
        Mock Invoke-Aapt2 { "package: name='app' versionCode='42' versionName='x'" }
    }

    It 'Publishes a Release APK with the version and copies it under a versioned name' {
        Mock Invoke-DotNet {
            $global:LASTEXITCODE = 0
            $script:Commands += $args[0]
            if ($args[0] -ne 'publish') { return }
            $script:PublishArguments = @($args)
            New-Item -ItemType Directory -Path $script:PublishDirectory -Force | Out-Null
            Set-Content -LiteralPath (Join-Path $script:PublishDirectory 'app-Signed.apk') -Value 'apk'
            'Build succeeded.'
        }

        $apk = @(Build-Apk -Root $root -Destination $destination -Code 42 -DisplayVersion '1.2.3-local.abc1234')

        $apk.Count | Should -Be 1 -Because 'build output must not leak into the result'
        $apk = $apk[0]

        $script:PublishArguments | Should -Contain '-p:AndroidPackageFormat=apk'
        $script:Commands | Should -Be @('clean', 'publish')
        $script:PublishArguments | Should -Contain '-p:ApplicationVersion=42'
        $script:PublishArguments | Should -Contain '-p:ApplicationDisplayVersion=1.2.3-local.abc1234'
        $script:PublishArguments | Should -Contain 'Release'
        $apk.Path | Should -Be (Join-Path $destination 'android-dcim-cloud-sync-1.2.3-local.abc1234-42.apk')
        Test-Path -LiteralPath $apk.Path | Should -BeTrue
        Test-Path -LiteralPath $script:PublishDirectory | Should -BeFalse
        $apk.Sha256 | Should -Match '^[0-9a-f]{64}$'
    }

    It 'Fails when dotnet publish fails' {
        Mock Invoke-DotNet { $global:LASTEXITCODE = if ($args[0] -eq 'publish') { 1 } else { 0 } }
        { Build-Apk -Root $root -Destination $destination -Code 42 -DisplayVersion '0.0.0-local' } | Should -Throw '*publish failed with exit code 1*'
    }

    It 'Fails when no signed APK is produced' {
        Mock Invoke-DotNet {
            New-Item -ItemType Directory -Path $script:PublishDirectory -Force | Out-Null
            $global:LASTEXITCODE = 0
        }
        { Build-Apk -Root $root -Destination $destination -Code 42 -DisplayVersion '0.0.0-local' } | Should -Throw '*found 0*'
    }

    It 'Fails when the packaged version code is stale' {
        Mock Invoke-DotNet {
            New-Item -ItemType Directory -Path $script:PublishDirectory -Force | Out-Null
            Set-Content -LiteralPath (Join-Path $script:PublishDirectory 'app-Signed.apk') -Value 'apk'
            $global:LASTEXITCODE = 0
        }
        Mock Invoke-Aapt2 { "package: name='app' versionCode='41' versionName='x'" }
        { Build-Apk -Root $root -Destination $destination -Code 42 -DisplayVersion '0.0.0-local' } | Should -Throw '*version code 41 instead of 42*'
    }

    It 'Fails when the app project is missing' {
        { Build-Apk -Root $TestDrive -Destination $destination -Code 42 -DisplayVersion '0.0.0-local' } | Should -Throw '*project was not found*'
    }

    It 'Builds nothing with -WhatIf' {
        Mock Invoke-DotNet { }
        Build-Apk -Root $root -Destination $destination -Code 42 -DisplayVersion '0.0.0-local' -WhatIf | Should -BeNullOrEmpty
        Should -Invoke Invoke-DotNet -Times 0
        Test-Path -LiteralPath $destination | Should -BeFalse
    }
}

Describe 'Install-Apk' {
    It 'Replaces the installed app on the chosen device' {
        Mock Invoke-Adb { $global:LASTEXITCODE = 0; 'Performing Streamed Install'; 'Success' }
        Install-Apk -Path 'app.apk' -Serial 'emulator-5554'
        Should -Invoke Invoke-Adb -Times 1 -ParameterFilter { ($args -join ' ') -eq '-s emulator-5554 install -r app.apk' }
    }

    It 'Omits the serial when none is given' {
        Mock Invoke-Adb { $global:LASTEXITCODE = 0; 'Success' }
        Install-Apk -Path 'app.apk'
        Should -Invoke Invoke-Adb -Times 1 -ParameterFilter { ($args -join ' ') -eq 'install -r app.apk' }
    }

    It 'Explains a signature mismatch' {
        Mock Invoke-Adb { $global:LASTEXITCODE = 1; 'Failure [INSTALL_FAILED_UPDATE_INCOMPATIBLE]' }
        { Install-Apk -Path 'app.apk' } | Should -Throw '*INSTALL_FAILED_UPDATE_INCOMPATIBLE*uninstalled first*'
    }

    It 'Installs nothing with -WhatIf' {
        Mock Invoke-Adb { }
        Install-Apk -Path 'app.apk' -WhatIf
        Should -Invoke Invoke-Adb -Times 0
    }
}
