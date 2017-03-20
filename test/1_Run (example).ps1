
Write-Host Finding test methods...

$mapping = '[{\"className\": \"HelloWorldJava.Demo.HelloWorldJunitOpt2Test\",\"methodName\": \"testTrue\",\"workItemID\": 216}]'
Start-Process ..\bin\UnitTestGenerator\Microsoft.DX.JavaTestBridge.UnitTestGenerator.exe -ArgumentList "AutomatedTestAssembly",`"$mapping`",.\examples -wait -RedirectStandardOutput unitgenerator_log.txt

Write-Host "C# DLL created: AutomatedTestAssembly.dll"

Start-Process ..\bin\VSTS\Microsoft.DX.JavaTestBridge.VSTS.exe -ArgumentList "https://gianlucabertelli.visualstudio.com","JavaTestBridge","AutomatedTestAssembly.dll","gianlucb","XXXXXXXXX" -wait -RedirectStandardOutput vsts_log.txt

Write-Host "VSTS Association done"
