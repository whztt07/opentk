﻿#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using OpenTK.Audio;

namespace Examples.OpenAL
{
    [Example("Streaming Playback", ExampleCategory.OpenAL)]
    public class StreamingPlayback
    {
        const string filename = "Data\\Audio\\the_ring_that_fell.wav";
        const int buffer_size = 18096;

        static object openal_lock = new object();

        public static void Main()
        {
            AudioContext context = new AudioContext();

            using (SoundReader sound = new SoundReader(filename))
            {
                int[] buffers = AL.GenBuffers(2);
                int source = AL.GenSource();
                int state;

                Console.WriteLine("Testing WaveReader({0}).ReadSamples()", filename);

                Console.Write("Playing");

                SoundStreamer streamer = new SoundStreamer(sound, source, buffers);
                streamer.Start();
                lock (openal_lock)
                {
                    AL.SourcePlay(source);
                }

                // Query the source to find out when it stops playing.
                do
                {
                    //Thread.Sleep(100);
                    //Console.Write(".");
                    lock (openal_lock)
                    {
                        AL.GetSource(source, ALGetSourcei.SourceState, out state);
                    }
                }
                while ((ALSourceState)state == ALSourceState.Playing);

                Console.WriteLine("asd");

                lock (openal_lock)
                {
                    AL.SourceStop(source);
                    AL.DeleteSource(source);
                    AL.DeleteBuffers(buffers);
                }

            }
        }

        class SoundStreamer
        {
            SoundReader reader;
            int source;
            int[] buffers;
            Thread thread;

            public SoundStreamer(SoundReader sound, int source, int[] buffers)
            {
                reader = sound;
                this.source = source;
                this.buffers = buffers;

                lock (openal_lock)
                {
                    foreach (int buffer in buffers)
                        AL.BufferData(buffer, sound.ReadSamples(buffer_size));

                    AL.SourceQueueBuffers(source, buffers.Length, buffers);
                }
            }

            public void Start()
            {
                thread = new Thread(new ThreadStart(StartStreaming));
                thread.Start();
            }

            void StartStreaming()
            {
                while (!reader.EndOfFile)
                {
                    lock (openal_lock)
                    {
                        int processed_count;
                        AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processed_count);
                        while (processed_count-- > 0)
                        {
                            int buffer = AL.SourceUnqueueBuffer(source);
                            AL.BufferData(buffer, reader.ReadSamples(buffer_size));
                            AL.SourceQueueBuffer(source, buffer);
                        }
                    }

                    Thread.Sleep(5);
                }
                Console.WriteLine("booh");
            }
        }
    }
}
