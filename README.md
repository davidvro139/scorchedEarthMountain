# Scorched Earth Mountain Creator

Desktop tool for creating and previewing Scorched Earth `.MTN` terrain files.

Made by David Everton on March 24, 2026.

## Overview

Scorched Earth uses `.MTN` files for scanned mountains, the battlefields that tanks fight across during a match. This project provides a Windows WPF application for converting standard images into `.MTN` terrain files and for decoding existing `.MTN` files back into bitmap previews.

This makes it possible to:

- Create custom battle terrain for Scorched Earth from your own source artwork
- Preview how an image will be reduced into the game's 16-color terrain format
- Inspect existing `.MTN` files by converting them back into `.bmp`

## What It Is Used For

In Scorched Earth, `.MTN` files define prebuilt landscapes that the game loads as playable terrain. Instead of fighting on randomly generated hills, players can battle on custom mountains built from image data.

This application is intended for:

- Modding or customizing Scorched Earth terrain
- Studying how legacy game image formats can be parsed and reconstructed
- Demonstrating binary file-format handling, image quantization, and desktop tooling in C#

## Features

- Convert an image into a Scorched Earth `.MTN` file
- Convert an existing `.MTN` file back into a 16-color bitmap
- Preview the prepared source image before export
- Preview the generated indexed bitmap used for terrain conversion
- Remove background regions before terrain export
- Use automatic top-left color detection or a manual detection color
- Export removed background as white sky in the final terrain data

## How It Works

The application follows the same core rules used by Scorched Earth terrain files:

1. A source image is loaded and converted into indexed 16-color image data.
2. The app builds a true 4-bit bitmap representation with a 16-entry palette.
3. Background can be removed by flood-filling edge-connected pixels that match the detected or selected background color within a configurable tolerance.
4. Removed background is exported as white sky, while the preview shows those removed regions as transparent for clarity.
5. The bitmap data is transformed into MTN column data, where each vertical slice stores only the non-sky pixels needed by the game.

The reverse path works in the opposite direction:

1. An `.MTN` file is parsed into header, palette, and pixel-column data.
2. The column data is padded back into a full image.
3. The image is reconstructed as a valid 16-color bitmap for preview or export.

## Project Structure

The WPF application lives in [`ScorchedEarthMountain.App`](./ScorchedEarthMountain.App).

- `Views`: WPF window and UI code-behind
- `Models`: MTN/BMP document models and shared data records
- `Services`: background removal, quantization, and preview generation
- `Utilities`: matrix and transformation helpers

## Running The App

```powershell
dotnet run --project .\ScorchedEarthMountain.App
```

## Image Preparation Notes

Scorched Earth treats sky as negative space above the terrain. For the cleanest results:

- Use images with a clearly separable background
- Keep the background connected to the edge of the image if you want it removed automatically
- Avoid concave enclosed spaces if you expect them to become sky, because enclosed areas may remain filled
- Expect the final result to be reduced to a 16-color palette

## Technical Notes

This project is a practical example of:

- Binary parsing and serialization
- Reverse-engineered file format implementation
- Palette-based image processing
- WPF desktop application development
- Converting legacy game data formats into modern tooling

## Attribution

This project is based in part on the reverse-engineering work documented by Zachary Ennenga in:

- [Please Destroy My Face: Reverse Engineering Scorched Earth's MTN File Format](https://zach-ennenga.medium.com/please-destroy-my-face-reverse-engineering-scorched-earths-mtn-file-format-e64a1a2c9b9f)
