Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$proc = Get-Process DataTable | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

$btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
Write-Host ("button count: " + $all.Count)
foreach ($b in $all) { Write-Host ("  button: '" + $b.Current.Name + "'") }
