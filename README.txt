# OpenAI-Compatible Dual Backend Chat

A .NET console application designed to provide a flexible chat interface that can switch between different OpenAI-compatible AI backends at runtime.

## Overview

This project leverages `Microsoft.Extensions.AI` to implement a provider-agnostic chat system. It is specifically designed to work with both Ollama and llama.cpp, ensuring that the application can utilize different models and backends based on availability and performance needs.

## Key Features

- **Dual Backend Support**: Full support for Ollama and llama.cpp via their OpenAI-compatible API layers.
- **Runtime Backend Switching**: Switch between AI providers (e.g., from Ollama to llama.cpp) and change model IDs on the fly using REPL commands without restarting the application.
- **Provider-Neutral Tool Calling**: A unified function registration system that allows the AI to call local tools (e.g., filesystem operations) regardless of the active backend.
- **Safety & Security**:
    - **Tool Exposure Policy**: An explicit allow-list for tools, ensuring high-risk operations (like shell execution) are disabled by default.
    - **Filesystem Sandboxing**: Restricts destructive file operations (write/move/delete) to a predefined sandbox root directory.
- **Standardized Client Layer**: Utilizes `Microsoft.Extensions.AI.OpenAI` to ensure consistent interaction with any backend that follows the OpenAI API specification.

## Configuration

The application uses the following default configurations:

### Ollama
- **Base URL**: `http://localhost:11434/v1`
- **Default Model**: `gemma4-dev:latest`
- **API Key**: Placeholder allowed.

### llama.cpp
- **Base URL**: `http://localhost:8080/v1`
- **Model ID**: Configurable independently.
- **API Key**: Configurable.

## Usage

The application runs as a REPL (Read-Eval-Print Loop) in the console.

### Commands
- `/provider <provider>`: Switch the active backend (e.g., `/provider ollama` or `/provider llama`).
- `/model <provider> <model_id>`: Update the model ID for a specific provider.
- `/exit`: Terminate the chat session.

## Technical Stack

- **Framework**: .NET
- **Libraries**: 
    - `Microsoft.Extensions.AI`
    - `Microsoft.Extensions.AI.OpenAI`

## Verification Matrix

The project is verified against the following criteria:
- [ ] Connectivity checks for both Ollama and llama.cpp endpoints.
- [ ] Plain chat functionality on both backends.
- [ ] End-to-end tool calling (create-read-delete) in a sandboxed environment.
- [ ] Seamless runtime switching between providers and models.
- [ ] Enforcement of the tool allow-list and sandbox restrictions.
