set serviceName=PostSoapCoreService
set serviceFilePath=%~dp0%PostSoapCoreService.exe
set serviceDescription=邮政分拣机Soap服务

sc create %serviceName%  BinPath=%serviceFilePath%
sc config %serviceName%    start=auto  
sc description %serviceName%  %serviceDescription%
sc start  %serviceName%
pause