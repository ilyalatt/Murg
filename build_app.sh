#!/bin/bash
set -e

rm -rf build
dotnet publish \
  --configuration Release \
  --runtime linux-x64 \
  /p:PublishSingleFile=true \
  --output build \
  src/Murg
dotnet restore # dotnet publish breaks dependencies
