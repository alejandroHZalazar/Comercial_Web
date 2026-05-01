using System.Globalization;
using System.Reflection;
using System.Text;

namespace Comercial_Web.Helpers
{
    /// <summary>
    /// Utilidad reutilizable para exportar listas de objetos a formato CSV.
    /// Uso: byte[] csv = CsvExporter.Generar(lista, columnas);
    /// </summary>
    public static class CsvExporter
    {
        /// <summary>
        /// Genera un CSV a partir de una lista de objetos y un diccionario de columnas.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto fuente</typeparam>
        /// <param name="datos">Lista de datos a exportar</param>
        /// <param name="columnas">
        /// Diccionario donde Key = nombre de la propiedad del objeto,
        /// Value = título del encabezado en el CSV
        /// </param>
        /// <param name="separador">Separador de columnas (default: punto y coma para Excel en es-AR)</param>
        /// <returns>Arreglo de bytes UTF-8 con BOM listo para descargar</returns>
        public static byte[] Generar<T>(
            IEnumerable<T> datos,
            Dictionary<string, string> columnas,
            string separador = ";")
        {
            var sb = new StringBuilder();
            var props = new List<PropertyInfo>();

            // Encabezados
            var headers = new List<string>();
            foreach (var col in columnas)
            {
                headers.Add(Escapar(col.Value, separador));
                var prop = typeof(T).GetProperty(col.Key, BindingFlags.Public | BindingFlags.Instance);
                props.Add(prop!);
            }
            sb.AppendLine(string.Join(separador, headers));

            // Filas
            foreach (var item in datos)
            {
                var valores = new List<string>();
                foreach (var prop in props)
                {
                    var valor = prop?.GetValue(item);
                    var texto = FormatearValor(valor);
                    valores.Add(Escapar(texto, separador));
                }
                sb.AppendLine(string.Join(separador, valores));
            }

            // UTF-8 con BOM para que Excel lo abra correctamente
            var bom = Encoding.UTF8.GetPreamble();
            var contenido = Encoding.UTF8.GetBytes(sb.ToString());
            var resultado = new byte[bom.Length + contenido.Length];
            bom.CopyTo(resultado, 0);
            contenido.CopyTo(resultado, bom.Length);

            return resultado;
        }

        /// <summary>
        /// Sobrecarga simplificada: las columnas se infieren de las propiedades del objeto.
        /// El nombre de la propiedad se usa como encabezado.
        /// </summary>
        public static byte[] Generar<T>(IEnumerable<T> datos, string separador = ";")
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnas = new Dictionary<string, string>();
            foreach (var p in props)
                columnas[p.Name] = p.Name;
            return Generar(datos, columnas, separador);
        }

        private static string FormatearValor(object? valor)
        {
            if (valor is null) return "";
            if (valor is decimal d) return d.ToString("N2", new CultureInfo("es-AR"));
            if (valor is double dbl) return dbl.ToString("N2", new CultureInfo("es-AR"));
            if (valor is DateTime dt) return dt.ToString("dd/MM/yyyy");
            if (valor is int i) return i.ToString();
            if (valor is long l) return l.ToString();
            return valor.ToString() ?? "";
        }

        private static string Escapar(string texto, string separador)
        {
            if (texto.Contains(separador) || texto.Contains('"') || texto.Contains('\n'))
                return $"\"{texto.Replace("\"", "\"\"")}\"";
            return texto;
        }
    }
}
