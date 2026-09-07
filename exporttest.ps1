Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$proc = Get-Process DataTable | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

$btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$btns = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
# buttons: [0]=row1-check [1]=row1-subsheets [2]=row1-export ...
$btn = $btns[2]
$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Host "clicked first row export"
Start-Sleep -Seconds 2

$dlgCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$dialogs = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $dlgCond)
$dlg = $null
foreach ($d in $dialogs) {
    Write-Host ("window: '" + $d.Current.Name + "' type=" + $d.Current.ControlType.ProgrammaticName)
    if ($d.Current.ControlType.ProgrammaticName -eq "ControlType.Window" -and $d.Current.Name -ne $root.Current.Name) { $dlg = $d }
}
if (-not $dlg) { Write-Host "no dialog"; exit 1 }

$dlgBtns = $dlg.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
Write-Host ("dialog buttons: " + $dlgBtns.Count)
# dialog buttons: [0]=browse-data [1]=browse-class [2]=export
$dlgBtns[2].GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Host "export invoked, waiting..."
Start-Sleep -Seconds 12

Get-ChildItem "C:\Project\DataTable\Result\CSharp" -Filter "D*.cs" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host ("CS: " + $_.Name + " " + $_.LastWriteTime) }
Get-ChildItem "C:\Project\DataTable\Result\Data\Json" -Filter "*.json" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host ("JSON: " + $_.Name + " " + $_.LastWriteTime) }
