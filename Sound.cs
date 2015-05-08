using System;
using System.IO;

using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace Brains
{
    class Sfx
    {
        private Sfx() { }

        public void Play()
        {
            if (IsPlaying()) return;

            AL.SourcePlay(Source);
        }

        public bool IsPlaying()
        {
            int state;

            AL.GetSource(Source, ALGetSourcei.SourceState, out state);
            return (ALSourceState)state == ALSourceState.Playing;
        }

        public void Unload()
        {
            AL.DeleteSource(Source);
            AL.DeleteBuffer(Buffer);

            Source = 0;
            Buffer = 0;
        }

        public int Source { get; private set; }
        public int Buffer { get; private set; }

        // This method was shamelessly copy pasted from OpenTK samples.
        public static Sfx Load(string filename)
        {
            Sfx result = new Sfx();

            result.Buffer = AL.GenBuffer();
            result.Source = AL.GenSource();

            Stream stream = File.Open(filename, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);

            reader.ReadChars(4); // RIFF?
            reader.ReadInt32(); // riff chunk size
            reader.ReadChars(4); // WAVE?
            reader.ReadChars(4); // "fmt "? Why so many checks!

            reader.ReadInt32(); // format chunk size
            reader.ReadInt16(); // audio format... -.-"
            int channels = reader.ReadInt16();
            int rate = reader.ReadInt32();
            reader.ReadInt32(); // byte rate ? WTF
            reader.ReadInt16();// block align
            int bps = reader.ReadInt16();

            reader.ReadChars(4); // some other useless stuff
            reader.ReadInt32(); // moar stuff

            byte[] data = reader.ReadBytes((int)reader.BaseStream.Length);

            // Double ternarity! All the way! What does it mean???
            ALFormat format = (channels == 1 ? bps == 8 ? ALFormat.Mono8 : ALFormat.Mono16 : bps == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16);

            AL.BufferData(result.Buffer, format, data, data.Length, rate);
            AL.Source(result.Source, ALSourcei.Buffer, result.Buffer);

            return result;
        }
    }
}
