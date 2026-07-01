# SpotlightSaver

SpotlightSaver is a small Windows utility that saves the current desktop wallpaper or Windows Spotlight image before it disappears.

Windows often shows beautiful Spotlight backgrounds with location information, but the image can rotate away before the user has a chance to save it. SpotlightSaver makes that easier.

## Features

- One-click save for the current Windows wallpaper
- Searches standard wallpaper and Windows Spotlight locations
- Saves images to Pictures\SpotlightSaver
- Creates a matching metadata text file
- Records capture time, source path, image dimensions, and file size
- Extracts basic local metadata when available
- Does not upload images
- Does not run in the background
- Does not use telemetry

## Save location

Images and text files are saved to:

%USERPROFILE%\Pictures\SpotlightSaver

## Privacy

SpotlightSaver is local-only. It does not upload wallpapers, metadata, filenames, or user data anywhere.

## Windows Spotlight limitation

Windows Spotlight often does not expose reliable location metadata locally. SpotlightSaver will preserve what it can find, but it will not guess.

## Build

Requires Windows and .NET 8 SDK or newer.

Build:

dotnet build .\src\SpotlightSaver\SpotlightSaver.csproj -c Release

Publish single EXE:

dotnet publish .\src\SpotlightSaver\SpotlightSaver.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish

## Portfolio note

This project demonstrates Windows automation, local file handling, metadata extraction, defensive programming, and practical user-focused tooling.

## Download

Download the latest Windows build here:

https://github.com/clintkosh/SpotlightSaver/releases/latest

Or view all releases:

https://github.com/clintkosh/SpotlightSaver/releases

The app is portable. Download the ZIP, unzip it, then double-click SpotlightSaver.exe.

Windows may show a SmartScreen warning because this is an unsigned independent utility.

## Local AI image analysis

SpotlightSaver can optionally ask a local Ollama vision model to analyze the saved wallpaper.

This is local-only. The app sends the saved image to `http://localhost:11434` on your own machine. It does not upload the image to a cloud service.

Supported local model auto-detection includes common Ollama vision models such as:

- llama3.2-vision
- llava
- bakllava
- moondream
- gemma3

Example setup:

ollama pull llama3.2-vision

or:

ollama pull llava

If Ollama is not running or no vision model is found, SpotlightSaver still saves the wallpaper and metadata file, then records that local AI analysis was unavailable.

AI location guesses are not facts. The metadata file labels AI analysis as best-effort visual inference.
