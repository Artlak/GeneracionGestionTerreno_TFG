using UnityEngine;

namespace AuxiliarClasses
{
    public class WorldVertex
    {
        public float height { get; set; }

        public Color color { get; set; }

        public float resistance { get; set; }

        public WorldVertex()
        {

        }

        public void Clear()
        {
            height = 0f;
            color = Color.white;
            resistance = 0f;
        }
    }

    public class Biome
    {
        public float[] heightMap { get; set; }

        public float[] resistanceMap { get; set; }

        public Gradient biomeColor { get; }

        // Patrón de rotura

        public Biome(int seed, int chunksSide)
        {
            System.Random prng = new System.Random(seed);
            biomeColor = GradientGenerator(prng);
            GenerateRandomNoiseMap(seed, chunksSide, prng);
        }

        public Biome(int seed, Gradient _biomeColor, int chunksSide)
        {
            biomeColor = _biomeColor;
            heightMap = Noise.GenerateNoiseMap(chunksSide, seed, 80, 16, 0.7f, 2, 1f, false);
            resistanceMap = Noise.GenerateNoiseMap(chunksSide, seed++ , 80, 16, 0.7f, 2, 0.5f, true);
        }

        public static Color ColorByResistance(Color heightColor, float _resistance)
        {
            Color newColor = Color.Lerp(heightColor, Color.black, _resistance);

            return newColor;
        }

        static Gradient GradientGenerator(System.Random prng)
        {
            Gradient _biomeColor = new Gradient();

            _biomeColor.mode = GradientMode.Fixed;

            GradientColorKey[] gck;
            GradientAlphaKey[] gak;

            gck = new GradientColorKey[4];
            gck[0].color = Color.HSVToRGB((float)prng.NextDouble(), (float)prng.NextDouble(), (float)prng.NextDouble());
            gck[0].time = .0f;
            gck[1].color = Color.HSVToRGB((float)prng.NextDouble(), (float)prng.NextDouble(), (float)prng.NextDouble());
            gck[1].time = .2f;
            gck[2].color = Color.HSVToRGB((float)prng.NextDouble(), (float)prng.NextDouble(), (float)prng.NextDouble());
            gck[2].time = .8f;
            gck[3].color = Color.HSVToRGB((float)prng.NextDouble(), (float)prng.NextDouble(), (float)prng.NextDouble());
            gck[3].time = 1.0f;
            gak = new GradientAlphaKey[2];
            gak[0].alpha = 1.0f;
            gak[0].time = 0.0f;
            gak[1].alpha = 1.0f;
            gak[1].time = 1.0f;

            _biomeColor.SetKeys(gck, gak);

            return _biomeColor;
        }

        // Por separar del constructor y que quede mejor organizado
        void GenerateRandomNoiseMap(int seed, int chunksSide, System.Random prng)
        {
            heightMap = Noise.GenerateNoiseMap(chunksSide, seed, 150f * (float)prng.NextDouble(), prng.Next(1, 16), (float)prng.NextDouble(), 1f + (float)prng.NextDouble(), (float)prng.NextDouble(), false);
            resistanceMap = Noise.GenerateNoiseMap(chunksSide, seed++, 150f * (float)prng.NextDouble(), prng.Next(1, 16), (float)prng.NextDouble(), 1f + (float)prng.NextDouble(), (float)prng.NextDouble(), true);
        }
    }
}