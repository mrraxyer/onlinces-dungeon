using System;
using System.IO;
using UnityEngine;

namespace OnLinces
{
    /// Progreso guardado entre partidas: en qué dificultades has ganado y la última dificultad elegida.
    /// Se guarda en Application.persistentDataPath/progreso.json
    public static class SaveSystem
    {
        [Serializable]
        public class Progress
        {
            public bool facil, media, dificil;
            public int lastDifficulty = (int)Difficulty.Media;
        }

        static Progress data;
        static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "progreso.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => data = null;

        public static Progress Data
        {
            get
            {
                if (data != null) return data;
                try { data = File.Exists(PathFile) ? JsonUtility.FromJson<Progress>(File.ReadAllText(PathFile)) : null; }
                catch (Exception e) { Debug.LogWarning("No se pudo leer el progreso: " + e.Message); }
                return data ??= new Progress();
            }
        }

        public static void Save()
        {
            try { File.WriteAllText(PathFile, JsonUtility.ToJson(Data, true)); }
            catch (Exception e) { Debug.LogWarning("No se pudo guardar el progreso: " + e.Message); }
        }

        public static bool HasWon(Difficulty d) => d switch { Difficulty.Facil => Data.facil, Difficulty.Media => Data.media, _ => Data.dificil };

        /// Ganar en una dificultad mayor también cuenta para las menores (como en Godot).
        public static void RegisterWin(Difficulty d)
        {
            if (d >= Difficulty.Facil) Data.facil = true;
            if (d >= Difficulty.Media) Data.media = true;
            if (d >= Difficulty.Dificil) Data.dificil = true;
            Save();
        }
    }
}
