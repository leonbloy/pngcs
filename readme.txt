 ==== PNGCS : A small library to read/write huge PNG files in C# ===

PngCs is a C# to read/write PNG images. 

It provides a simple API for progressive (sequential line-oriented) reading and writing. 
It's specially suitable for huge images, which one does not want to load fully in memory.

It supports all PNG spec color models and bitdepths: RGB8/RGB16/RGBA8/RGBA16, G8/4/2/1,
GA8/4/2/1, PAL8/4/2/1,  all filters/compressions settings. It does not support interlaced images. It also has support for Chunks (metadata).

This is a port of the PngJ library (Java):
http://code.google.com/p/pngj/
the API, documentation and samples from PNGJ apply also
to this PngCs library: http://code.google.com/p/pngj/wiki/Overview

--------------------------------------------------------------

NOTE: PngCs includes some source code from the library SharpZipLib
http://www.icsharpcode.net/opensource/sharpziplib/
with minor modifications. That library is originally 
released under the GPL licence, and so is this PNGCS library.

-----------------------------------------------------------------------------

History: 


22/Sept/2011: Implemented functionality is on par with current PngJ, 
some testing, optimizing and testing pending.

21/Sept/2011: Added classes from SharpZipLib, basic reading and writing works

Jan/2011: Basic port, aided by http://j2cstranslator.sourceforge.net/ (nice tool!)

Hernan J Gonzalez - hgonzalez@gmail.com -  http://stackoverflow.com/users/277304/leonbloy

---------------------------------------------------------------------------------