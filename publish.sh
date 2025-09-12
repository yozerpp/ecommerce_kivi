#!/bin/bash
cd "/mnt/c/Users/tipil/RiderProjects/Ecommerce/Ecommerce.WebImpl/bin/Release/net9.0/linux-x64/publish"
echo "compressing files"
tar -cjf dll.tar.bz2 $(ls | egrep -v -e '^wwwroot$')
tar -cjf wwwroot.tar.bz2 wwwroot/
echo "decompressing files"
scp -r *.tar.bz2 root@185.223.77.167:/usr/local/server/
ssh root@185.223.77.167 tar -xjf /usr/local/server/wwwroot.tar.bz2 -C /usr/local/server/
ssh root@185.223.77.167 tar -xjf /usr/local/server/dll.tar.bz2 -C /usr/local/server/