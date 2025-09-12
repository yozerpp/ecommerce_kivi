dotnet publish Ecommerce.WebImpl --os linux -a x64 -c Release --no-self-contained
ssh root@185.223.77.167 bash -c "/root/shutdown.sh"
wsl -e bash "./publish.sh"
ssh root@185.223.77.167 bash -c "/usr/local/server/start.sh"