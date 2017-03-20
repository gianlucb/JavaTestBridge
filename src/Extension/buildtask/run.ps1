[CmdletBinding()]
param()

# For more information on the VSTS Task SDK:
# https://github.com/Microsoft/vsts-task-lib
Trace-VstsEnteringInvocation $MyInvocation
try {

    # Get the inputs.
    [string]$testResultPath = Get-VstsInput -Name testResultPath
    [string]$username = Get-VstsInput -Name username
    [string]$password = Get-VstsInput -Name password
    [string]$outputFolder = Get-VstsInput -Name outputFolder
    [string]$jsonMapping = Get-VstsInput -Name jsonMapping  
    [string]$inputType = Get-VstsInput -Name inputType
    if($inputType -eq "file"){
              $jsonMapping = Get-VstsInput -Name jsonMappingFile
              Write-host "Using file $jsonMapping for JSON mapping"
    }
    else
    {
        Write-host "-----------------------"
        Write-host "Json test mapping"
        Write-host "-----------------------"
        Write-host `"$jsonMapping`"
    }

    
    $vsoAccountName = (Get-VstsTaskVariableInfo | Where-Object { $_.Name -eq "system.taskDefinitionsUri"}).Value
    $projectName = (Get-VstsTaskVariableInfo | Where-Object { $_.Name -eq "build.Repository.name"}).Value

    Write-host "-----------------------"
    Write-host "Config"
    Write-host "-----------------------"
    Write-host "Tests path= $testResultPath"
    Write-host "Project name (var)= $projectName"
    Write-host "Visual Studio Account name (var)= $vsoAccountName"
    Write-host "Output folder= $outputFolder"

    #Set the working directory.
    $cwd = "bin"
    Assert-VstsPath -LiteralPath $cwd -PathType Container
    Write-host "Setting working directory to '$cwd'."
    Set-Location $cwd

    Write-host "Creation of dynamic .NET DLL using ROSLYN..."
    Write-host "-------------------------------------------------"

    Invoke-VstsTool -FileName ".\Microsoft.DX.JavaTestBridge.UnitTestGenerator.exe" -Arguments "AutomatedTestAssembly `"$jsonMapping`" $testResultPath" -RequireExitCodeZero
    
    if($LASTEXITCODE -eq 0){

        Write-host ".NET Unit test assembly created (AutomatedTestAssembly.dll)"
        
        Write-host "Association of tests with VSTS..."
        Write-host "-------------------------------------------------"

        Invoke-VstsTool -FileName ".\Microsoft.DX.JavaTestBridge.VSTS.exe" -Arguments "$vsoAccountName $projectName AutomatedTestAssembly.dll $username $password" -RequireExitCodeZero
        if($LASTEXITCODE -eq 0)
        {
          Write-host "Association completed successfully"
         
          #required DLL to run
          Move-Item .\AutomatedTestAssembly.dll $outputFolder -Force
          Copy-Item .\Newtonsoft.Json.dll $outputFolder -Force
          Copy-Item .\Microsoft.DX.JavaTestBridge.Common.dll $outputFolder -Force

          Write-host "Files copied to $outputFolder"
        }
        else
        {
            Write-Error "Association of tests failed"
        }
    }
    else
    {
      Write-Error "Creation of .NET dynamic test DLL failed";
    }

   
} finally {
    Trace-VstsLeavingInvocation $MyInvocation
}