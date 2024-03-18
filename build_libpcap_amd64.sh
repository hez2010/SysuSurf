#!/bin/sh

wget https://www.tcpdump.org/release/libpcap-1.10.4.tar.xz
tar xvf libpcap-1.10.4.tar.xz
cd libpcap-1.10.4
apt install flex bison
./configure --enable-remote --disable-universal
make
cd ..
