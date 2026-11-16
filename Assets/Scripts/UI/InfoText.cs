using System.Linq;

namespace OnLinces
{
    /// Arma el texto del cuadro de descripción por apartados (TextMeshPro rich text):
    ///   descripción
    ///   HABILIDAD          ← encabezado pequeño color café
    ///   Suplencia · 13 s
    ///   Escudo, 2.5 s invulnerable...
    public static class InfoText
    {
        const string HeaderColor = "#8A5A2B";
        const string LabelColor = "#6B4A2B";
        const string PriceColor = "#8C5A00";

        /// Apartado con encabezado: "HABILIDAD" arriba y el contenido abajo.
        public static string Section(string header, string body) =>
            $"<size=78%><color={HeaderColor}><b><cspace=1>{header.ToUpperInvariant()}</cspace></b></color></size>\n{body}";

        /// Une apartados con un pequeño espacio entre ellos (ignora los vacíos).
        public static string Join(params string[] parts) =>
            string.Join("\n<size=45%> </size>\n", parts.Where(p => !string.IsNullOrEmpty(p)));

        /// "Etiqueta: valor" con la etiqueta en tono más suave.
        public static string Stat(string label, string value) => $"<color={LabelColor}>{label}:</color> {value}";

        /// Nombre resaltado (ej. el nombre de la habilidad).
        public static string Strong(string text) => $"<b>{text}</b>";

        /// Precio grande y dorado.
        public static string Price(int price) => $"<size=115%><color={PriceColor}><b>${price}</b></color></size>";
    }
}
