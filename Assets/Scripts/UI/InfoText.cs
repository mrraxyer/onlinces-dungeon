using System.Linq;

namespace OnLinces
{
    /// Rich text del diálogo por apartados.
    public static class InfoText
    {
        const string HeaderColor = "#FFD84D";
        const string LabelColor = "#A9A4BF";

        /// Encabezado y contenido.
        public static string Section(string header, string body) =>
            $"<color={HeaderColor}><cspace=1>{header.ToUpperInvariant()}</cspace></color>\n{body}";

        /// Une apartados no vacíos con espacio.
        public static string Join(params string[] parts) =>
            string.Join("\n<size=45%> </size>\n", parts.Where(p => !string.IsNullOrEmpty(p)));

        /// "Etiqueta: valor"
        public static string Stat(string label, string value) => $"<color={LabelColor}>{label}:</color> {value}";

        public static string Strong(string text) => $"<b>{text}</b>";
    }
}
