if ! command -v protoc &> /dev/null
then
    echo "protoc is not installed."
    exit
fi

mkdir -p InnerTube/Protobuf
cd Protobuf
protoc --csharp_out=../InnerTube/Protobuf *.proto
cd ..
