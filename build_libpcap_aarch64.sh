#!/bin/sh

wget https://www.tcpdump.org/release/libpcap-1.10.4.tar.xz
tar xvf libpcap-1.10.4.tar.xz
wget https://musl.cc/aarch64-linux-musl-cross.tgz
tar zxvf aarch64-linux-musl-cross.tgz
export CC=$(pwd)/aarch64-linux-musl-cross/bin/aarch64-linux-musl-gcc
cd libpcap-1.10.4
./configure --host aarch64 --enable-remote --disable-universal
make
cd ..
