#!/bin/sh

wget https://www.tcpdump.org/release/libpcap-1.10.4.tar.xz
tar xvf libpcap-1.10.4.tar.xz
wget https://musl.cc/aarch64-linux-musl-cross.tgz
tar zxvf aarch64-linux-musl-cross.tgz
wget https://www.openssl.org/source/openssl-3.0.13.tar.gz
tar xzvf openssl-3.0.13.tar.gz
wget https://www.zlib.net/zlib-1.3.1.tar.gz
tar xzvf zlib-1.3.1.tar.gz

export CC=$(pwd)/aarch64-linux-musl-cross/bin/aarch64-linux-musl-gcc

cd libpcap-1.10.4
./configure --host aarch64 --enable-remote --disable-universal
make -j4
cd ..

cd openssl-3.0.13
./config linux-aarch64 no-tests # shared
make -j4
# cp libssl.so.3 /crossrootfs/arm64/usr/lib/
# cp libcrypto.so.3 /crossrootfs/arm64/usr/lib/
cp libssl.a /crossrootfs/arm64/usr/lib/
cp libcrypto.a /crossrootfs/arm64/usr/lib/
cd ..

cd zlib-1.3.1
./configure
make -j4
cp libz.a /crossrootfs/arm64/usr/lib/
cd ..
