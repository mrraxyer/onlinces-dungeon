using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Pantalla final: "¡GANASTE!" o "GAME OVER", tu personaje y un resumen de la partida
    /// (puesto, dificultad, hasta dónde llegaste, monedas e ítems).
    public sealed class EndScreen : MonoBehaviour
    {
        static readonly Color WinColor = new(0.32f, 0.85f, 0.45f);
        static readonly Color LoseColor = new(0.95f, 0.32f, 0.35f);

        [SerializeField] TMP_Text resultLabel;
        [SerializeField] TMP_Text subtitleLabel;
        [Tooltip("Cuadro de pergamino con el resumen")]
        [SerializeField] TMP_Text summaryLabel;
        [SerializeField] Image character;
        [SerializeField] Sprite onSprite;
        [SerializeField] Sprite offSprite;
        [Tooltip("Pisos en orden (para escribir 'Piso 2: Sala de Servidores')")]
        [SerializeField] FloorData[] floors;

        void Start()
        {
            var run = GameManager.Instance.Run;
            bool won = run.Won == true;

            resultLabel.text = won ? "¡GANASTE!" : "GAME OVER";
            resultLabel.color = won ? WinColor : LoseColor;
            subtitleLabel.text = won ? "Venciste los 3 pisos del calabozo" : $"Caíste en {FloorName(run.Floor)}";

            if (character)
            {
                character.sprite = run.Mascot == Mascot.Off ? offSprite : onSprite;
                character.color = won ? Color.white : new Color(0.65f, 0.65f, 0.75f); // apagado si perdiste
            }

            string role = run.Role ? run.Role.displayName : "Miembro";
            int reached = won ? SceneFlow.LastFloor : run.Floor;
            summaryLabel.text = InfoText.Join(
                InfoText.Section("Tu partida",
                    $"{InfoText.Stat("Mascota", run.Mascot.ToString())}\n" +
                    $"{InfoText.Stat("Puesto", role)}\n" +
                    $"{InfoText.Stat("Dificultad", DifficultyInfo.Name(run.Difficulty))}"),
                InfoText.Section("Resultado",
                    $"{InfoText.Stat("Pisos superados",$"{(won ? reached : reached - 1)} de {SceneFlow.LastFloor}")}\n" +
                    $"{InfoText.Stat("Monedas", "$" + run.Coins)}  ·  {InfoText.Stat("Ítems", run.Items.Count.ToString())}"));
        }

        string FloorName(int n)
        {
            if (floors != null && n >= 1 && n <= floors.Length && floors[n - 1])
                return $"Piso {n}: {floors[n - 1].displayName}";
            return $"Piso {n}";
        }
    }
}
