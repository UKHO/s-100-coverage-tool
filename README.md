---
title: S-100 Coverage Tool
description: Reads an S-100 exchange catalogue and finds a compatible collection of S-104, S-111, S-101, and S-102 datasets by geographic coverage.
---

## Purpose

The S-100 Coverage Tool reads an S-100 Exchange Set Catalogue and finds the first
compatible collection of datasets for the same geographic area. It is a .NET
console application.

For a collection to match, the tool requires:

* The S-104 and S-111 coverage areas overlap
* The combined S-101 coverage contains both areas
* The combined S-102 coverage contains both areas

The tool uses coverage polygons when present and falls back to the catalogue
bounding box when a dataset has no polygon. It stops searching after it finds
the first matching collection.

## Requirements

* .NET 10 SDK

## Run

From the project root, run the tool with the default catalogue and exclusion
list:

```powershell
dotnet run
```

By default, the application reads `data/CATALOG.XML` and
`data/excluded-products.txt`. You can provide alternate paths as arguments:

```powershell
dotnet run -- path\to\CATALOG.XML path\to\excluded-products.txt
```

The first argument is the catalogue path. The second argument is optional; if
omitted, the default exclusion list is used. Relative paths are resolved from
the current working directory.

The console reports how many datasets were loaded and excluded, then shows
progress through the S-104/S-111 pairs. When a match is found, it prints the
S-104, S-111, S-101, and S-102 dataset filenames. If no match exists, it reports
that no matching collection was found.

## Exclude Datasets

Edit `data/excluded-products.txt` to exclude datasets. Add one dataset filename
per line, without its path or extension. Matching is case-insensitive and
exact. For example, an entry such as `104XY0061111N22222W`
matches that filename regardless of its directory or extension.

Blank lines and lines beginning with `#` are ignored. Every dataset whose
filename matches an entry is excluded before the coverage search.