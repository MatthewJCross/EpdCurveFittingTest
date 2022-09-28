using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpdCurveFittingTest
{
    public class SignalGeneratorExt
    {
        // Random Number for the White Noise & Pink Noise Generator
        private readonly Random random = new Random();

        private readonly double[] pinkNoiseBuffer = new double[7];

        // Const Math
        private const double TwoPi = 2 * Math.PI;

        // Generator variable
        private int nSample;

        // Sweep Generator variable
        private double phi;

        /// <summary>
        /// Initializes a new instance for the Generator (Default :: 44.1Khz, 2 channels, Sinus, Frequency = 440, Gain = 1)
        /// </summary>
        public SignalGeneratorExt() : this(44100, 2)
        {
        }

        /// <summary>
        /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
        /// </summary>
        /// <param name="sampleRate">Desired sample rate</param>
        /// <param name="channel">Number of channels</param>
        public SignalGeneratorExt(int sampleRate, int channel)
        {
            phi = 0;

            // Default
            Type = SignalGeneratorTypeExt.Sin;
            Frequency = 440.0;
            Gain = 1;
            PhaseReverse = new bool[channel];
            SweepLengthSecs = 2;
        }

        /// <summary>
        /// The waveformat of this WaveProvider (same as the source)
        /// </summary>

        /// <summary>
        /// Frequency for the Generator. (20.0 - 20000.0 Hz)
        /// Sin, Square, Triangle, SawTooth, Sweep (Start Frequency).
        /// </summary>
        public double Frequency { get; set; }

        /// <summary>
        /// Return Log of Frequency Start (Read only)
        /// </summary>
        public double FrequencyLog => Math.Log(Frequency);

        /// <summary>
        /// End Frequency for the Sweep Generator. (Start Frequency in Frequency)
        /// </summary>
        public double FrequencyEnd { get; set; }

        /// <summary>
        /// Return Log of Frequency End (Read only)
        /// </summary>
        public double FrequencyEndLog => Math.Log(FrequencyEnd);

        /// <summary>
        /// Gain for the Generator. (0.0 to 1.0)
        /// </summary>
        public double Gain { get; set; }

        /// <summary>
        /// Channel PhaseReverse
        /// </summary>
        public bool[] PhaseReverse { get; set; }

        public double PhaseOffset { get; set; }

        /// <summary>
        /// Type of Generator.
        /// </summary>
        public SignalGeneratorTypeExt Type { get; set; }

        /// <summary>
        /// Length Seconds for the Sweep Generator.
        /// </summary>
        public double SweepLengthSecs { get; set; }

        /// <summary>
        /// Reads from this provider.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int outIndex = offset;

            // Generator current value
            double multiple;
            double sampleValue;
            double sampleSaw;

            // Complete Buffer
            for (int sampleCount = 0; sampleCount < count / 1; sampleCount++)
            {
                switch (Type)
                {
                    case SignalGeneratorTypeExt.Sin:

                        // Sinus Generator

                        multiple = TwoPi * Frequency / 44100;
                        sampleValue = Gain * Math.Sin(nSample * multiple);

                        nSample++;

                        break;


                    case SignalGeneratorTypeExt.Square:

                        // Square Generator

                        multiple = 2 * Frequency / 44100;
                        sampleSaw = ((nSample * multiple) % 2) - 1;
                        sampleValue = sampleSaw > 0 ? Gain : -Gain;

                        nSample++;
                        break;

                    case SignalGeneratorTypeExt.Triangle:

                        // Triangle Generator

                        multiple = 2 * Frequency / 44100;
                        sampleSaw = ((nSample * multiple) % 2);
                        sampleValue = 2 * sampleSaw;
                        if (sampleValue > 1)
                            sampleValue = 2 - sampleValue;
                        if (sampleValue < -1)
                            sampleValue = -2 - sampleValue;

                        sampleValue *= Gain;

                        nSample++;
                        break;

                    case SignalGeneratorTypeExt.SawTooth:

                        // SawTooth Generator

                        multiple = 2 * Frequency / 44100;
                        sampleSaw = ((nSample * multiple) % 2) - 1;
                        sampleValue = Gain * sampleSaw;

                        nSample++;
                        break;

                    case SignalGeneratorTypeExt.White:

                        // White Noise Generator
                        sampleValue = (Gain * NextRandomTwo());
                        break;

                    case SignalGeneratorTypeExt.Pink:

                        // Pink Noise Generator

                        double white = NextRandomTwo();
                        pinkNoiseBuffer[0] = 0.99886 * pinkNoiseBuffer[0] + white * 0.0555179;
                        pinkNoiseBuffer[1] = 0.99332 * pinkNoiseBuffer[1] + white * 0.0750759;
                        pinkNoiseBuffer[2] = 0.96900 * pinkNoiseBuffer[2] + white * 0.1538520;
                        pinkNoiseBuffer[3] = 0.86650 * pinkNoiseBuffer[3] + white * 0.3104856;
                        pinkNoiseBuffer[4] = 0.55000 * pinkNoiseBuffer[4] + white * 0.5329522;
                        pinkNoiseBuffer[5] = -0.7616 * pinkNoiseBuffer[5] - white * 0.0168980;
                        double pink = pinkNoiseBuffer[0] + pinkNoiseBuffer[1] + pinkNoiseBuffer[2] + pinkNoiseBuffer[3] + pinkNoiseBuffer[4] + pinkNoiseBuffer[5] + pinkNoiseBuffer[6] + white * 0.5362;
                        pinkNoiseBuffer[6] = white * 0.115926;
                        sampleValue = (Gain * (pink / 5));
                        break;

                    case SignalGeneratorTypeExt.Sweep:

                        // Sweep Generator
                        double f = Math.Exp(FrequencyLog + (nSample * (FrequencyEndLog - FrequencyLog)) / (SweepLengthSecs * 44100));

                        multiple = TwoPi * f / 44100;
                        phi += multiple;
                        sampleValue = Gain * (Math.Sin(phi));
                        nSample++;
                        if (nSample > SweepLengthSecs * 44100)
                        {
                            nSample = 0;
                            phi = 0;
                        }
                        break;

                    default:
                        sampleValue = 0.0;
                        break;
                }

                // Phase Reverse Per Channel
                for (int i = 0; i < 1; i++)
                {
                    if (PhaseReverse[i])
                        buffer[outIndex++] = (float)-sampleValue;
                    else
                        buffer[outIndex++] = (float)sampleValue;

                    //if (outIndex >= count)
                    //outIndex = 0;

                    if (sampleCount >= count)
                        break;
                }
            }

            if (PhaseOffset < 0)
            {
                int samples = -(int)Math.Round(44100 / 261.6, 0);
                //outIndex = count - (int)(samples * PhaseOffset);
                RotateArray.Rotate(buffer, (int)(samples * Math.Abs(PhaseOffset)));
            }
            else if (PhaseOffset > 0)
            {
                int samples = (int)Math.Round(44100 / 261.6, 0);
                //outIndex = (int)(samples * PhaseOffset);
                RotateArray.Rotate(buffer, (int)(samples * PhaseOffset));
            }

            return count;
        }

        /// <summary>
        /// Private :: Random for WhiteNoise &amp; Pink Noise (Value form -1 to 1)
        /// </summary>
        /// <returns>Random value from -1 to +1</returns>
        private double NextRandomTwo()
        {
            return 2 * random.NextDouble() - 1;
        }

    }

    public static class RotateArray
    {
        public static void Rotate<T>(this T[] array, int count)
        {
            if (array == null || array.Length < 2) return;
            count %= array.Length;
            if (count == 0) return;
            int left = count < 0 ? -count : array.Length + count;
            int right = count > 0 ? count : array.Length - count;
            if (left <= right)
            {
                for (int i = 0; i < left; i++)
                {
                    var temp = array[0];
                    Array.Copy(array, 1, array, 0, array.Length - 1);
                    array[array.Length - 1] = temp;
                }
            }
            else
            {
                for (int i = 0; i < right; i++)
                {
                    var temp = array[array.Length - 1];
                    Array.Copy(array, 0, array, 1, array.Length - 1);
                    array[0] = temp;
                }
            }
        }
    }

    /// <summary>
    /// Signal Generator type
    /// </summary>
    public enum SignalGeneratorTypeExt
    {
        /// <summary>
        /// Pink noise
        /// </summary>
        Pink,
        /// <summary>
        /// White noise
        /// </summary>
        White,
        /// <summary>
        /// Sweep
        /// </summary>
        Sweep,
        /// <summary>
        /// Sine wave
        /// </summary>
        Sin,
        /// <summary>
        /// Square wave
        /// </summary>
        Square,
        /// <summary>
        /// Triangle Wave
        /// </summary>
        Triangle,
        /// <summary>
        /// Sawtooth wave
        /// </summary>
        SawTooth,

        FatSawTooth
    }
}
