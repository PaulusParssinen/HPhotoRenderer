# HPhotoRenderer
A tiny CLI app to convert old Habbo Hotel MUS camera payloads to images written in .NET.

## Background
In late 2019, [@Quackster](https://github.com/Quackster) asked me to reverse-engineer a Shockwave Multiuser Server (MUS in short) camera payload sent by the Habbo Hotel shockwave client so I wrote an example how to do it in C#, which they then [ported to Java](https://github.com/Quackster/PhotoRenderer). 
This is a updated version of the original version I wrote.

## Building
**Prerequisities:** .NET 8 SDK
```sh
git clone https://github.com/PaulusParssinen/HPhotoRenderer.git
cd ./HPhotoRenderer/HPhotoRenderer
dotnet build
dotnet run [PATH_TO_MUS_BINARY]
```

## Todo
* Properly blend the grayscale version like the game client does
* Make it a tiny library instead
