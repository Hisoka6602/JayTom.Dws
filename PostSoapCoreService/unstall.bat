set serviceName=PostSoapCoreService

sc stop   %serviceName% 
sc delete %serviceName% 

pause