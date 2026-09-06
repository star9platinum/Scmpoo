using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Scmpoo.Modern.Services;

public sealed class SoundService : IDisposable
{
    private const uint Async = 0x0001;
    private const uint NoDefault = 0x0002;
    private const uint Memory = 0x0004;
    private const uint Loop = 0x0008;
    private const uint FileName = 0x00020000;
    private const uint MinimumPlayIntervalMilliseconds = 200;

    private readonly object gate = new();
    private readonly AutoResetEvent requestReady = new(false);
    private readonly Dictionary<int, GCHandle> resources = new();
    private Thread? worker;
    private SoundRequest pending;
    private bool hasPending;
    private bool disposed;
    private bool hasAcceptedPlay;
    private uint lastAcceptedPlay;

    private enum RequestKind { Resource, File, Stop, Shutdown }

    private readonly struct SoundRequest
    {
        public RequestKind Kind { get; }
        public int ResourceId { get; }
        public bool Looping { get; }
        public string? Path { get; }

        public SoundRequest(RequestKind kind, int resourceId = 0, bool looping = false,
            string? path = null)
        {
            Kind = kind;
            ResourceId = resourceId;
            Looping = looping;
            Path = path;
        }
    }

    public void Play(int resourceId, bool loop = false) => TryPlay(resourceId, loop);

    public bool TryPlay(int resourceId, bool loop = false)
    {
        if (resourceId < 108 || resourceId > 110) return false;
        return Queue(new SoundRequest(RequestKind.Resource, resourceId, loop));
    }

    public void PlayFile(string path) => TryPlayFile(path);

    public bool TryPlayFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return Queue(new SoundRequest(RequestKind.File, path: path));
    }

    public void Stop() => Queue(new SoundRequest(RequestKind.Stop));

    private bool Queue(SoundRequest request)
    {
        lock (gate)
        {
            if (disposed) return false;
            if (request.Kind == RequestKind.Stop && worker == null) return false;
            if (request.Kind is RequestKind.Resource or RequestKind.File)
            {
                uint now = unchecked((uint)Environment.TickCount);
                if (hasAcceptedPlay && unchecked(now - lastAcceptedPlay) < MinimumPlayIntervalMilliseconds)
                    return false;
                hasAcceptedPlay = true;
                lastAcceptedPlay = now;
            }

            if (worker == null)
            {
                worker = new Thread(Run) { IsBackground = true, Name = "Scmpoo audio" };
                try
                {
                    worker.Start();
                }
                catch (Exception error)
                {
                    worker = null;
                    Debug.WriteLine(error);
                    return false;
                }
            }

            // A slow sound device retains only the latest accepted request.
            pending = request;
            hasPending = true;
            requestReady.Set();
            return true;
        }
    }

    private void Run()
    {
        try
        {
            while (true)
            {
                requestReady.WaitOne();
                SoundRequest request;
                lock (gate)
                {
                    if (!hasPending) continue;
                    request = pending;
                    hasPending = false;
                }

                try
                {
                    switch (request.Kind)
                    {
                        case RequestKind.Resource:
                            IntPtr resource = GetResource(request.ResourceId);
                            if (resource != IntPtr.Zero)
                                PlayMemory(resource, IntPtr.Zero,
                                    Async | Memory | NoDefault | (request.Looping ? Loop : 0));
                            break;
                        case RequestKind.File:
                            PlayFileNative(request.Path!, IntPtr.Zero, Async | FileName | NoDefault);
                            break;
                        case RequestKind.Stop:
                            PlayMemory(IntPtr.Zero, IntPtr.Zero, 0);
                            break;
                        case RequestKind.Shutdown:
                            return;
                    }
                }
                catch (Exception error)
                {
                    // Failed or unavailable audio never terminates animation.
                    Debug.WriteLine(error);
                }
            }
        }
        finally
        {
            bool stopped = false;
            try
            {
                stopped = PlayMemory(IntPtr.Zero, IntPtr.Zero, 0);
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
            // WinMM keeps SND_MEMORY pointers after PlaySound returns. Pins
            // are released only after confirmed cancellation on this worker.
            if (stopped)
            {
                foreach (GCHandle resource in resources.Values) resource.Free();
                resources.Clear();
            }
            requestReady.Close();
        }
    }

    private IntPtr GetResource(int resourceId)
    {
        if (resources.TryGetValue(resourceId, out GCHandle existing))
            return existing.AddrOfPinnedObject();

        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "Scmpoo.Assets." + resourceId + ".wav");
        if (stream == null || stream.Length <= 0 || stream.Length > int.MaxValue)
            return IntPtr.Zero;

        byte[] bytes = new byte[(int)stream.Length];
        int position = 0;
        while (position < bytes.Length)
        {
            int count = stream.Read(bytes, position, bytes.Length - position);
            if (count == 0) throw new EndOfStreamException("Incomplete embedded sound.");
            position += count;
        }
        GCHandle pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        resources.Add(resourceId, pinned);
        return pinned.AddrOfPinnedObject();
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            if (worker == null)
            {
                requestReady.Close();
                return;
            }
            pending = new SoundRequest(RequestKind.Shutdown);
            hasPending = true;
            requestReady.Set();
        }
        // A background thread owns cancellation and resource cleanup. Joining
        // here would allow an unresponsive audio driver to block application exit.
    }

    [DllImport("winmm.dll", EntryPoint = "PlaySoundA", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlayMemory(IntPtr sound, IntPtr module, uint flags);

    [DllImport("winmm.dll", EntryPoint = "PlaySound", CharSet = CharSet.Auto,
        ExactSpelling = false, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlayFileNative(string path, IntPtr module, uint flags);
}
