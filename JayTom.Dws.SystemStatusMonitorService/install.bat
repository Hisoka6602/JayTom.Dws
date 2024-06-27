set serviceName=JayTom.Dws.SystemStatusMonitorService
set serviceFilePath=%~dp0%JayTom.Dws.SystemStatusMonitorService.exe
set serviceDescription=系统信息推送服务

sc create %serviceName%  BinPath=%serviceFilePath%
sc config %serviceName%    start=auto  
sc description %serviceName%  %serviceDescription%
sc start  %serviceName%
pause