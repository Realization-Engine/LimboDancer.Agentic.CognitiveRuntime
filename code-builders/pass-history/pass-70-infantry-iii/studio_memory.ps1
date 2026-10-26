# Pass 70's memory reading of a Studio (fix Round D, fixes_boundary.md 1): the private and working-set memory of the process that listens on a port,
# read after the page has opened a game and handed over. A coarse reading (no forced collection), the same way on each build.
#   pwsh -File studio_memory.ps1 <port>
param([int]$Port)
$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -First 1
$process = Get-Process -Id $listener.OwningProcess
"port {0}: pid {1}, private {2:N0} MB, working set {3:N0} MB, GC heap n/a" -f $Port, $process.Id, ($process.PrivateMemorySize64 / 1MB), ($process.WorkingSet64 / 1MB)
