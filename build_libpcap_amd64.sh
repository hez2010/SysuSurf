#!/bin/sh

wget https://www.tcpdump.org/release/libpcap-1.10.4.tar.xz
tar xvf libpcap-1.10.4.tar.xz
cd libpcap-1.10.4
./configure --enable-remote --disable-universal
make -j4
cd ..
