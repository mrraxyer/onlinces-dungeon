using System.Linq;

namespace OnLinces
{
    /// Rich text del diálogo por apartados.
    public static class InfoText
    {
        const string HeaderColor = "#8A5A2B";
        const string LabelColor = "#6B4A2B";

        /// Encabezado y contenido.
        public static string Section(string header, string body) =>
            $"<size=78%><color={HeaderColor}><b><cspace=1>{header.ToUpperInvariant()}</cspace></b></color></size>\n{body}";

        /// Une apartados no vacíos con espacio.
        public static string Join(params string[] parts) =>
            string.Join("\n<size=45%> </size>\n", parts.Where(p => !string.IsNullOrEmpty(p)));

        /// "Etiqueta: valor"
        public static string Stat(string label, string value) => $"<color={LabelColor}>{label}:</color> {value}";

        public static string Strong(string text) => $"<b>{text}</b>";
    }
}
