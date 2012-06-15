 ==== PNGCS : A small library to read/write huge PNG files in C# ===

PngCs is a C# to read/write PNG images. 

It provides a simple API for progressive (sequential line-oriented) reading and writing. 
It's specially suitable for huge images, which one does not want to load fully in memory.

It supports all PNG spec color models and bitdepths: RGB8/RGB16/RGBA8/RGBA16, G8/4/2/1,
GA8/4/2/1, PAL8/4/2/1,  all filters/compressions settings. It does not support interlaced images. 
It also has support for Chunks (metadata).

This is a port of the PngJ library (Java): http://code.google.com/p/pngj/
the API, documentation and samples from PNGJ apply also
to this PngCs library: http://code.google.com/p/pngj/wiki/Overview

The distribution of this library includes documentation in folder docs/

See also the included sample projects, 

--------------------------------------------------------------

NOTE: PngCs depends on SharpZipLib http://www.icsharpcode.net/opensource/sharpziplib/
The ICSharpCode.SharpZipLib.dll assembly, provided with this library,
and must be referenced together with Pngcs.dll by client projects.
Because SharpZipLib is released  under the GPL license with an exception
that allows to link it with independent modules, PNGCS relies on that
exception and is released under the Apache license. See LICENSE.txt

-----------------------------------------------------------------------------

History: 

See changes.txt

Hernan J Gonzalez - hgonzalez@gmail.com -  http://stackoverflow.com/users/277304/leonbloy

---------------------------------------------------------------------------------