set serviceName=JayTom.Dws.SystemStatusMonitorService

sc stop   %serviceName% 
sc delete %serviceName% 

pause