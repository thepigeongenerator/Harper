#!/bin/bash

if [ -d MinecraftServerApplication/bin/Release/net8.0/linux-x64 ]; then
    rm -rfv MinecraftServerApplication/bin/Release/net8.0/linux-x64
fi

dotnet build --configuration Release --self-contained True

if [ ! -d ./package ]; then
    echo "\033[91mcould not find the package directory\033[0m"
    exit -1
fi

cd package
rm -rfv ./harper/usr/bin/harper/*
cp -r ../MinecraftServerApplication/bin/Release/net8.0/linux-x64/* ./harper/usr/bin/harper
mv ./harper/usr/bin/harper/settings ./harper/etc/harper
echo "" > ./harper/etc/harper/bot_token.txt
dpkg-deb --build harper/
