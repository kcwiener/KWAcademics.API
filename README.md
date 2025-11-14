# KWAcademics.API

A .NET 9 Web API that transforms written text into spoken audio using Azure Speech Services. This application allows users to adjust speech speed and voice type, and download the generated audio files for offline use.

## Features

- **Text-to-Speech Conversion**: Transform any text into natural-sounding speech
- **Adjustable Speech Rate**: Control the speed of speech using prosody rate (-50 to +100)
- **Voice Selection**: Choose from multiple Azure neural voices
- **Audio Download**: Download generated audio as MP3 files (128kbps)
- **REST API Architecture**: Lightweight, platform-independent implementation using Azure Speech REST API
- **Azure AD Authentication**: Secure API access with Microsoft identity platform

## Architecture

This application uses **Azure Speech REST API** instead of the Speech SDK, providing:
- No native DLL dependencies
- Platform-independent deployment (works on 32-bit and 64-bit)
- Smaller deployment size (~10 MB vs 190 MB)
- No architecture compatibility issues
- Simplified project configuration

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Speech Service](https://azure.microsoft.com/services/cognitive-services/speech-services/) subscription
- [Azure AD](https://azure.microsoft.com/services/active-directory/) tenant (for authentication)

### Installation

1. **Clone the repository**
