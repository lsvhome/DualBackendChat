#!/bin/bash


OllamaSharp=/home/ubuntu/git/ollamasharp
echo $OllamaSharp

rm -rf ${OllamaSharp}
cp -r /home/ubuntu/git/ollama-intel-arc/workspace/OllamaSharp ${OllamaSharp}

#OLLAMA_HOST=http://localhost:11434 dotnet run --project ${OllamaSharp}/demo/OllamaApiConsole.csproj


OLLAMA_HOST=http://localhost:11434 dotnet run --project /home/ubuntu/git/ollama-intel-arc/workspace/OllamaSharp/demo/OllamaApiConsole.csproj
