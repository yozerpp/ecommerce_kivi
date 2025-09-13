dotnet publish Ecommerce.WebImpl --os linux -a x64 -c Release --no-self-contained
ssh root@$PERSONAL_SERVER bash -c "/root/shutdown.sh"
wsl -e bash "./publish.sh"
ssh root@PERSONAL_SERVER bash -c "/usr/local/server/start.sh"
