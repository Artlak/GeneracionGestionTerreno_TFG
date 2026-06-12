using UnityEngine;

namespace AuxiliarClasses
{
    [System.Serializable]
    public class WorldVertex
    {
        public float height;

        public Color color;

        public float resistance;

        public WorldVertex()
        {

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
            heightMap = Noise.GenerateNoiseMap(chunksSide, seed, 80, 16, 0.7f, 1, 1f, false);
            seed++;
            resistanceMap = Noise.GenerateNoiseMap(chunksSide, seed, 80, 16, 0.7f, 1, 0.5f, true);
        }

        public static Color ColorByResistance(Color heightColor, float _resistance)
        {
            Color newColor = Color.Lerp(heightColor, Color.black, _resistance * .5f);

            return newColor;
        }

        static Gradient GradientGenerator(System.Random prng)
        {
            Gradient _biomeColor = new Gradient();

            _biomeColor.mode = GradientMode.Fixed;

            float baseColor = (float)prng.NextDouble();

            GradientColorKey[] gck;
            GradientAlphaKey[] gak;

            float s = Mathf.Lerp(0.5f, 1f, (float)prng.NextDouble());
            float v = Mathf.Lerp(0.4f, 1f, (float)prng.NextDouble());

            gck = new GradientColorKey[4];
            for (int i = 0; i < gck.Length; i++)
            {
                float h = Mathf.Lerp(baseColor, (float)prng.NextDouble(), (float)prng.NextDouble());
                
                gck[i].color = Color.HSVToRGB(h,s,v);
                gck[i].time = (float) i / (gck.Length - 1);
            }
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