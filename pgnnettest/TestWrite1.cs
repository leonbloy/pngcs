using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ar.Com.Hjg.Pngcs;

namespace pgncstest
{
    class TestWrite1
    {
        	/**
	 * crea imagen de test: primera linea negra, segunda blanca. primera columna
	 * amarilla, ultima verde. Degrade de colores, y alpha transparente abajo a la
	 * izquierda
	 */
	private static void makeTestImage(PngWriter png) {
		int cols = png.imgInfo.cols;
		int rows = png.imgInfo.rows;
		bool alpha = png.imgInfo.alpha;
		// int bitspc = png.imgInfo.bitDepth;
		int channels = png.imgInfo.channels;
		int valuesPerRow = png.imgInfo.samplesPerRow;
		ImageLine iline = new ImageLine(png.imgInfo);
		iline.SetRown(0);
		ImageLineHelper.SetValD(iline, 0, 1.0);
		ImageLineHelper.SetValD(iline, 1, 1.0); // primer columna amarilla
		ImageLineHelper.SetValD(iline, 2, 0.0);
		ImageLineHelper.SetValD(iline, valuesPerRow - channels, 0);
		ImageLineHelper.SetValD(iline, valuesPerRow - channels + 1, 1.0); // ultima
																																			// columna
																																			// verde
		ImageLineHelper.SetValD(iline, valuesPerRow - channels + 2, 0);
		for (int j = 1; j < cols - 1; j++) { // primera fila: blanca
			ImageLineHelper.SetValD(iline, j * channels, 1.0);
			ImageLineHelper.SetValD(iline, j * channels + 1, 1.0);
			ImageLineHelper.SetValD(iline, j * channels + 2, 1.0);
		}
		if (alpha)
			addAlpha(iline);
		png.WriteRow(iline);
		iline.IncRown();
		for (int j = 1; j < cols - 1; j++) { // segunda fila: negra
			ImageLineHelper.SetValD(iline, j * channels, 0.0);
			ImageLineHelper.SetValD(iline, j * channels + 1, 0.0);
			ImageLineHelper.SetValD(iline, j * channels + 2, 0.0);
		}
		if (alpha)
			addAlpha(iline);
		png.WriteRow(iline);
		iline.IncRown();
		for (; iline.GetRown() < rows; iline.IncRown()) {
			for (int j = 1; j < cols - 1; j++) {
				ImageLineHelper.SetValD(iline, j * channels, clamp((2 * j / cols) - 0.3, 0, 1.0));
				ImageLineHelper.SetValD(iline, j * channels + 1,
						clamp((2 * iline.GetRown() / rows) - 0.4, 0, 1.0));
				ImageLineHelper.SetValD(
						iline,
						j * channels + 2,
						clamp(
								(0.55 * Math.Sin(13.0 * iline.GetRown() / rows + j * 25.0 / cols) + 0.5),
								0, 1.0));
			}
			if (alpha)
				addAlpha(iline);
			png.WriteRow(iline);
		}
	}

	private static void addAlpha(ImageLine iline) {
		int cols = iline.imgInfo.cols;
		int rows = iline.imgInfo.rows;
		for (int i = 0; i < iline.imgInfo.cols; i++) {
			double alpha;
			if (i == 0 || i == iline.imgInfo.cols - 1 || iline.GetRown() < 2)
				alpha = 1.0;
			else {
				// opaco arriba a la derecha, transparente abajo izquierda
				double d = Math.Sqrt(((0.5 * i) / cols + 0.0)
						+ ((0.5 * (rows - iline.GetRown())) / rows + 0.0)); // entre 0 y 1
				d = d * 1.3 - 0.2;
				alpha = clamp(d, 0.0, 1.0);
			}
			ImageLineHelper.SetValD(iline, i * 4 + 3, clamp(alpha, 0, 1.0)); // asume
																																				// que
																																				// son 4
																																				// canales!
		}
	}

	private static double clamp(double d, double d0, double d1) {
		return d > d1 ? d1 : (d < d0 ? d0 : d);
	}

	public static void createTest1(String orig, int cols, int rows, int bitspc, int channels)
			 {
		if (channels != 3 && channels != 4)
			throw new Exception("bad channels number (must be 3 or 4)");
		PngWriter i2 =  new PngWriter(new FileStream (orig, FileMode.CreateNew),
             new ImageInfo(cols, rows,
				bitspc, channels == 4), orig);
		makeTestImage(i2);
		i2.End(); // cierra el archivo
        Console.WriteLine("Done: " + i2.GetFilename());
	}

	public static void createBlackRGB8(String filename,int cols, int rows)
	 {
                 PngWriter png = new PngWriter(new FileStream(filename, FileMode.Create), 
                     new ImageInfo(cols, rows, 8, false), filename);
		ImageLine iline = new ImageLine(png.imgInfo);
		for (int j = 0; j < cols ; j++) 
			ImageLineHelper.SetPixelRGB8(iline, j, 0);
		for (int row=0;row<rows; row++) {
			iline.SetRown(row);
			png.WriteRow(iline);
		}
		png.End();
	}

    public static void Run()
    {
        createBlackRGB8("C:\\temp\\test.png", 30, 50);
        Console.WriteLine("Listo!");

    }
    }

    
}
