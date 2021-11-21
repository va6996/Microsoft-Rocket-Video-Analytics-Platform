#! /bin/bash

apt-get -y update

export DEBIAN_FRONTEND=noninteractive

# to change time zone to EST
export TZ="America/New_York"
apt-get install tzdata
rm -rf /etc/localtime
cp -rp /usr/share/zoneinfo/EST /etc/localtime

apt-get -y install git curl wget make build-essential cmake
apt-get -y install perl sqlite3 libswiss-perl libxml-parser-perl
apt-get -y install libxml2 libxml2-dev

apt-get -y install build-essential git emacs wget curl libjpeg62 vim
apt-get -y install gfortran flex bison
apt-get -y install autoconf automake

apt-get -y install lsb-core
apt-get -y install libexpat1-dev
apt-get -y install tcsh zsh

apt-get -y install iproute2 strace uuid-runtime

apt-get clean

wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt-get update; apt-get install -y apt-transport-https && apt-get update && apt-get install -y dotnet-sdk-5.0
apt-get update; apt-get install -y apt-transport-https && apt-get update && apt-get install -y aspnetcore-runtime-5.0
apt-get install -y dotnet-runtime-5.0

apt-get install -y tesseract-ocr libc6-dev libgdiplus python3-pip ffmpeg libc6-dev libgdiplus libgtk2.0-0
