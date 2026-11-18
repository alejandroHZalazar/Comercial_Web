using System.Globalization;
using System.Reflection;
using Domain.Contracts;
using Domain.DTO;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Infrastructure.Services;

/// <summary>
/// Genera el PDF "Listado de Stock en Existencia" con PDFsharp (100 % administrado, sin dependencias
/// nativas ni fuentes del sistema, apto para el contenedor Linux). El paginado se calcula antes de
/// dibujar para poder mostrar "Página X de Y"; el encabezado y el pie se repiten en cada página.
/// </summary>
public class StockExistenciaPdfService : IStockExistenciaPdfService
{
    // ── Página A4 apaisada (puntos) ──────────────────────────────────────────
    private const double PageW = 841.89, PageH = 595.28;
    private const double HeaderH = 80;           // alto del encabezado (incluye la línea inferior)
    private const double FooterH = 34;           // alto reservado al pie
    private const double Margin  = 28;

    // ── Tabla ────────────────────────────────────────────────────────────────
    private static readonly (string Titulo, double W, bool Der)[] Cols =
    {
        ("Código",        65,  false),
        ("Descripción",   175, false),
        ("Rubro",         102, false),
        ("Stock",         51,  true),
        ("Costo unit.",   68,  true),
        ("P. lista unit.", 71, true),
        ("Total costo",   85,  true),
        ("Total venta",   85,  true),
        ("Rentabilidad",  84,  true),
    };
    private static readonly double TableW = Cols.Sum(c => c.W);
    private const double PadX = 3, PadY = 3, LineH = 10, MinRowH = 16, HeadRowH = 18, TotRowH = 22;

    private static readonly XColor Navy   = XColor.FromArgb(26, 42, 74);
    private static readonly XColor Alt    = XColor.FromArgb(242, 245, 251);
    private static readonly XColor Grid   = XColor.FromArgb(221, 227, 238);
    private static readonly XColor Grey   = XColor.FromArgb(85, 85, 85);
    private static readonly XColor Red    = XColor.FromArgb(176, 42, 31);

    // Formato monetario/numérico argentino fijo (no depende de los datos de cultura del servidor)
    private static readonly NumberFormatInfo Nf = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator   = ".",
        NumberGroupSizes       = new[] { 3 },
        NegativeSign           = "-"
    };

    public byte[] Generar(StockExistenciaReporteDto d)
    {
        PdfFonts.Registrar();

        var fReg  = new XFont("Lato", 8,   XFontStyleEx.Regular);
        var fBold = new XFont("Lato", 8,   XFontStyleEx.Bold);
        var rows  = d.Filas;

        // Totales con los mismos valores (ya redondeados) que se muestran en el detalle
        var totUnidades = rows.Sum(r => r.Stock);
        var totCosto    = rows.Sum(r => r.TotalCosto);
        var totVenta    = rows.Sum(r => r.TotalVenta);
        var totRent     = rows.Sum(r => r.Rentabilidad);

        // ── Pasada 1: medir filas y repartirlas en páginas ──────────────────
        using var scratch = new PdfDocument();
        using var mg = XGraphics.FromPdfPage(scratch.AddPage());

        var layouts = rows.Select(r => MedirFila(mg, fReg, r, d.DecimalesStock)).ToList();

        var bodyTop    = Margin + HeaderH;
        var bodyBottom = PageH - Margin - FooterH;
        var pages = new List<(int From, int To, bool Totales)>();   // To exclusivo

        if (rows.Count > 0)
        {
            var i = 0;
            while (i < rows.Count)
            {
                var y = bodyTop + HeadRowH;
                var from = i;
                while (i < rows.Count && (i == from || y + layouts[i].Height <= bodyBottom))
                {
                    y += layouts[i].Height;
                    i++;
                }
                pages.Add((from, i, false));
            }
            // La fila de totales va a continuación de la última fila; si no entra, pasa a una página nueva
            var last = pages[^1];
            var usado = bodyTop + HeadRowH + layouts.Skip(last.From).Take(last.To - last.From).Sum(l => l.Height);
            if (usado + TotRowH <= bodyBottom) pages[^1] = (last.From, last.To, true);
            else pages.Add((rows.Count, rows.Count, true));
        }
        else
        {
            pages.Add((0, 0, false));
        }

        // ── Pasada 2: dibujar ───────────────────────────────────────────────
        using var doc = new PdfDocument();
        doc.Info.Title   = "Listado de Stock en Existencia";
        doc.Info.Creator = string.IsNullOrWhiteSpace(d.EmpresaNombre) ? "Gestión Comercial" : d.EmpresaNombre;

        XImage? logo = null;
        if (d.Logo is { Length: > 1 })
        {
            try { logo = XImage.FromStream(new MemoryStream(d.Logo)); }
            catch { logo = null; }       // formato no soportado: el reporte sale igual, sin logo
        }

        var fecha = d.FechaGeneracion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        for (var p = 0; p < pages.Count; p++)
        {
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            using var g = XGraphics.FromPdfPage(page);

            DibujarEncabezado(g, d, logo, fecha, rows.Count);
            DibujarPie(g, d, logo, fecha, p + 1, pages.Count);

            if (rows.Count == 0)
            {
                var msg = "No hay productos con stock en existencia (stock mayor a cero).";
                g.DrawString(msg, new XFont("Lato", 11, XFontStyleEx.Regular), new XSolidBrush(Grey),
                    new XRect(Margin, bodyTop + 40, PageW - 2 * Margin, 20), Centro);
                continue;
            }

            var x0 = (PageW - TableW) / 2;
            var y = bodyTop;
            DibujarTitulosColumnas(g, x0, y, fBold);
            y += HeadRowH;

            var (from, to, totales) = pages[p];
            for (var i = from; i < to; i++)
            {
                DibujarFila(g, x0, y, layouts[i], fReg, fBold, i % 2 == 1);
                y += layouts[i].Height;
            }

            if (totales)
                DibujarTotales(g, x0, y, fBold, rows.Count, d.DecimalesStock, totUnidades, totCosto, totVenta, totRent);
        }

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        logo?.Dispose();
        return ms.ToArray();
    }

    // ── Medición ─────────────────────────────────────────────────────────────
    private sealed class FilaLayout
    {
        public string[][] Lineas { get; init; } = Array.Empty<string[]>();   // por columna; vacío si es numérica
        public string[]   Textos { get; init; } = Array.Empty<string>();     // texto de cada columna
        public decimal[]  Valores { get; init; } = Array.Empty<decimal>();   // importes (columnas 4..8)
        public double     Height  { get; init; }
    }

    private static FilaLayout MedirFila(XGraphics g, XFont f, StockExistenciaDto r, int decStock)
    {
        var textos = new[]
        {
            r.Codigo, r.Descripcion, r.Rubro,
            r.Stock.ToString("N" + decStock, Nf),
            Dinero(r.Costo), Dinero(r.PrecioLista),
            Dinero(r.TotalCosto), Dinero(r.TotalVenta), Dinero(r.Rentabilidad)
        };
        var valores = new[] { r.Stock, r.Costo, r.PrecioLista, r.TotalCosto, r.TotalVenta, r.Rentabilidad };

        var lineas = new string[Cols.Length][];
        var maxLineas = 1;
        for (var c = 0; c < Cols.Length; c++)
        {
            if (Cols[c].Der) { lineas[c] = Array.Empty<string>(); continue; }
            lineas[c] = Partir(g, f, textos[c], Cols[c].W - 2 * PadX);
            maxLineas = Math.Max(maxLineas, lineas[c].Length);
        }

        return new FilaLayout
        {
            Lineas  = lineas,
            Textos  = textos,
            Valores = valores,
            Height  = Math.Max(MinRowH, maxLineas * LineH + 2 * PadY)
        };
    }

    /// <summary>Parte el texto en líneas que entren en el ancho dado (por palabras; las muy largas, por carácter).</summary>
    private static string[] Partir(XGraphics g, XFont f, string texto, double ancho)
    {
        texto = (texto ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        if (texto.Length == 0) return new[] { string.Empty };

        var lineas = new List<string>();
        var actual = string.Empty;

        void Cerrar() { if (actual.Length > 0) { lineas.Add(actual); actual = string.Empty; } }

        foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var prueba = actual.Length == 0 ? palabra : actual + " " + palabra;
            if (g.MeasureString(prueba, f).Width <= ancho) { actual = prueba; continue; }

            Cerrar();
            if (g.MeasureString(palabra, f).Width <= ancho) { actual = palabra; continue; }

            // Palabra más ancha que la celda: se corta por caracteres
            var trozo = string.Empty;
            foreach (var ch in palabra)
            {
                if (g.MeasureString(trozo + ch, f).Width > ancho && trozo.Length > 0)
                {
                    lineas.Add(trozo);
                    trozo = string.Empty;
                }
                trozo += ch;
            }
            actual = trozo;
        }
        Cerrar();
        return lineas.ToArray();
    }

    // ── Dibujo ───────────────────────────────────────────────────────────────
    private static readonly XStringFormat Izq    = new() { Alignment = XStringAlignment.Near,   LineAlignment = XLineAlignment.Near };
    private static readonly XStringFormat Der    = new() { Alignment = XStringAlignment.Far,    LineAlignment = XLineAlignment.Near };
    private static readonly XStringFormat Centro = new() { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Near };

    private static void DibujarEncabezado(XGraphics g, StockExistenciaReporteDto d, XImage? logo, string fecha, int cantidad)
    {
        var navy   = new XSolidBrush(Navy);
        var grey   = new XSolidBrush(Grey);
        var xTexto = Margin;

        if (logo != null)
        {
            const double maxW = 120, maxH = 58;
            var ratio = logo.PointWidth / logo.PointHeight;
            var w = maxW; var h = w / ratio;
            if (h > maxH) { h = maxH; w = h * ratio; }
            g.DrawImage(logo, Margin, Margin + 2, w, h);
            xTexto = Margin + maxW + 12;
        }

        var anchoIzq = PageW - Margin - 330 - xTexto;
        g.DrawString(d.EmpresaNombre, new XFont("Lato", 14, XFontStyleEx.Bold), navy,
            new XRect(xTexto, Margin, anchoIzq, 18), Izq);
        g.DrawString(d.EmpresaRazon, new XFont("Lato", 9, XFontStyleEx.Regular), grey,
            new XRect(xTexto, Margin + 20, anchoIzq, 12), Izq);
        g.DrawString(d.EmpresaDireccion, new XFont("Lato", 8, XFontStyleEx.Regular), grey,
            new XRect(xTexto, Margin + 34, anchoIzq, 11), Izq);
        g.DrawString(d.EmpresaContacto, new XFont("Lato", 8, XFontStyleEx.Regular), grey,
            new XRect(xTexto, Margin + 46, anchoIzq, 11), Izq);

        var xDer = PageW - Margin - 320;
        g.DrawString("Listado de Stock en Existencia", new XFont("Lato", 15, XFontStyleEx.Bold), navy,
            new XRect(xDer, Margin, 320, 20), Der);
        var f85 = new XFont("Lato", 8.5, XFontStyleEx.Regular);
        g.DrawString("Generado: " + fecha, f85, grey, new XRect(xDer, Margin + 22, 320, 12), Der);
        g.DrawString("Productos informados: " + cantidad.ToString("N0", Nf), f85, grey, new XRect(xDer, Margin + 35, 320, 12), Der);
        g.DrawString("Importes sin IVA", f85, grey, new XRect(xDer, Margin + 48, 320, 12), Der);

        g.DrawLine(new XPen(Navy, 2), Margin, Margin + HeaderH - 12, PageW - Margin, Margin + HeaderH - 12);
    }

    private static void DibujarPie(XGraphics g, StockExistenciaReporteDto d, XImage? logo, string fecha, int pagina, int total)
    {
        var y = PageH - Margin - FooterH + 8;
        var grey = new XSolidBrush(Grey);
        g.DrawLine(new XPen(XColor.FromArgb(184, 194, 214), 0.75), Margin, y, PageW - Margin, y);

        var xTexto = Margin;
        if (logo != null)
        {
            var h = 20.0; var w = h * logo.PointWidth / logo.PointHeight;
            if (w > 50) { w = 50; h = w * logo.PointHeight / logo.PointWidth; }
            g.DrawImage(logo, Margin, y + 4, w, h);
            xTexto = Margin + w + 8;
        }

        g.DrawString(d.EmpresaNombre, new XFont("Lato", 8, XFontStyleEx.Bold), new XSolidBrush(Navy),
            new XRect(xTexto, y + 9, 260, 11), Izq);
        g.DrawString("Emitido el " + fecha, new XFont("Lato", 8, XFontStyleEx.Regular), grey,
            new XRect(PageW / 2 - 100, y + 9, 200, 11), Centro);
        g.DrawString($"Página {pagina} de {total}", new XFont("Lato", 8, XFontStyleEx.Regular), grey,
            new XRect(PageW - Margin - 120, y + 9, 120, 11), Der);
    }

    private static void DibujarTitulosColumnas(XGraphics g, double x0, double y, XFont fBold)
    {
        var x = x0;
        var fondo = new XSolidBrush(Navy);
        var blanco = XBrushes.White;
        for (var c = 0; c < Cols.Length; c++)
        {
            g.DrawRectangle(fondo, x, y, Cols[c].W, HeadRowH);
            g.DrawString(Cols[c].Titulo, fBold, blanco,
                new XRect(x + PadX, y + 5, Cols[c].W - 2 * PadX, 10), Cols[c].Der ? Der : Izq);
            x += Cols[c].W;
        }
    }

    private static void DibujarFila(XGraphics g, double x0, double y, FilaLayout fila, XFont fReg, XFont fBold, bool alterna)
    {
        if (alterna)
            g.DrawRectangle(new XSolidBrush(Alt), x0, y, TableW, fila.Height);
        g.DrawLine(new XPen(Grid, 0.5), x0, y + fila.Height, x0 + TableW, y + fila.Height);

        var x = x0;
        for (var c = 0; c < Cols.Length; c++)
        {
            if (!Cols[c].Der)
            {
                var ty = y + PadY;
                foreach (var linea in fila.Lineas[c])
                {
                    g.DrawString(linea, fReg, XBrushes.Black,
                        new XRect(x + PadX, ty, Cols[c].W - 2 * PadX, LineH), Izq);
                    ty += LineH;
                }
            }
            else
            {
                // Importes: una sola línea, centrados verticalmente; si no entran se reduce la fuente
                var negativo = fila.Valores[c - 3] < 0;
                var fuente = c == Cols.Length - 1 ? fBold : fReg;
                var brush = negativo ? new XSolidBrush(Red) : XBrushes.Black;
                DibujarNumero(g, fila.Textos[c], fuente, brush, x, y + (fila.Height - LineH) / 2, Cols[c].W);
            }
            x += Cols[c].W;
        }
    }

    private static void DibujarTotales(XGraphics g, double x0, double y, XFont fBold, int cantidad, int decStock,
        decimal unidades, decimal costo, decimal venta, decimal rent)
    {
        var fondo = new XSolidBrush(Navy);
        g.DrawRectangle(fondo, x0, y, TableW, TotRowH);

        var ty = y + (TotRowH - LineH) / 2;
        var wLabel = Cols[0].W + Cols[1].W + Cols[2].W;
        g.DrawString($"TOTALES GENERALES  ({cantidad.ToString("N0", Nf)} productos)", fBold, XBrushes.White,
            new XRect(x0 + PadX, ty, wLabel - 2 * PadX, LineH), Izq);

        var x = x0 + wLabel;
        DibujarNumero(g, unidades.ToString("N" + decStock, Nf), fBold, XBrushes.White, x, ty, Cols[3].W);
        x += Cols[3].W + Cols[4].W + Cols[5].W;
        DibujarNumero(g, Dinero(costo), fBold, XBrushes.White, x, ty, Cols[6].W);   x += Cols[6].W;
        DibujarNumero(g, Dinero(venta), fBold, XBrushes.White, x, ty, Cols[7].W);   x += Cols[7].W;
        DibujarNumero(g, Dinero(rent),  fBold, XBrushes.White, x, ty, Cols[8].W);
    }

    /// <summary>Dibuja un número alineado a la derecha en una sola línea, reduciendo la fuente si no entra.</summary>
    private static void DibujarNumero(XGraphics g, string texto, XFont fuente, XBrush brush, double x, double y, double ancho)
    {
        var disponible = ancho - 2 * PadX;
        var f = fuente;
        var size = fuente.Size;
        while (g.MeasureString(texto, f).Width > disponible && size > 5)
        {
            size -= 0.25;
            f = new XFont("Lato", size, fuente.Style);
        }
        g.DrawString(texto, f, brush, new XRect(x + PadX, y, disponible, LineH), Der);
    }

    private static string Dinero(decimal v) =>
        (v < 0 ? "-$ " : "$ ") + Math.Abs(v).ToString("N2", Nf);

    // ── Fuentes embebidas (Lato, licencia OFL) ──────────────────────────────
    private sealed class PdfFonts : IFontResolver
    {
        private static readonly object Candado = new();
        private static bool _registrado;

        public static void Registrar()
        {
            lock (Candado)
            {
                if (_registrado) return;
                GlobalFontSettings.FontResolver = new PdfFonts();
                _registrado = true;
            }
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "Lato-Bold" : "Lato-Regular");

        public byte[]? GetFont(string faceName)
        {
            var asm = typeof(PdfFonts).Assembly;
            var recurso = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(faceName + ".ttf", StringComparison.OrdinalIgnoreCase));
            if (recurso == null) return null;

            using var s = asm.GetManifestResourceStream(recurso)!;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
