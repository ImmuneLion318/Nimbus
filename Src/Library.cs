using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Nimbus.Native;

namespace Nimbus;

public class Options
{
    public string Name = "Nimbus";
}

public struct ClientInfo
{
    public string Version;
    public string Name;
    public int Id;
}

public class Library : IDisposable
{
    internal Options Options;

    internal ExecuteAsyncDelegate ExecuteAsync;
    internal GetClientsDelegate GetClients;
    internal InitializeDelegate Initialize;

    internal string ModuleName => $"{this.Options.Name}-Module.dll";

    public Library(Options Options)
    {
        this.Options = Options;

        Dictionary<string, string> Dependencies = new Dictionary<string, string>
        {
            { this.ModuleName, "https://github.com/CloudyExecugor/frontend/releases/download/reareaaaa/Cloudy.dll" },
            { "libcrypto-3-x64.dll", "https://github.com/CloudyExecugor/frontend/releases/download/reareaaaa/libcrypto-3-x64.dll" },
            { "libssl-3-x64.dll", "https://github.com/CloudyExecugor/frontend/releases/download/reareaaaa/libssl-3-x64.dll" },
            { "xxhash.dll", "https://github.com/CloudyExecugor/frontend/releases/download/reareaaaa/xxhash.dll" },
            { "zstd.dll", "https://github.com/CloudyExecugor/frontend/releases/download/reareaaaa/zstd.dll" },
        };

        foreach (KeyValuePair<string, string> Files in Dependencies)
        {
            if (File.Exists(Files.Key))
                continue;

            using (WebClient Http = new WebClient { Proxy = null })
                Http.DownloadFile(Files.Value, Files.Key);

            if (!File.Exists(Files.Key))
                throw new NimbusException($"Failed To Download Dependency: {Files.Key}");
        }

        this.Initialize = Resolve<InitializeDelegate>(this.ModuleName, "Initialize");
        this.GetClients = Resolve<GetClientsDelegate>(this.ModuleName, "GetClients");
        this.ExecuteAsync = Resolve<ExecuteAsyncDelegate>(this.ModuleName, "ExecuteAsync");
    }

    public void Inject()
    {
        if (this.Options is null)
            throw new NimbusException("Options Is Null");

        if (Process.GetProcessesByName("RobloxPlayerBeta").Length is 0)
            throw new NimbusException("RobloxPlayerBeta Not Found");

        this.Initialize();

        if (this.Clients is null || this.Clients.Count is 0)
            Thread.Sleep(TimeSpan.FromSeconds(1));
    }

    public void Execute(string Script)
    {
        if (Script is null || Script.Length is 0)
            throw new NimbusException("Script Is Null Or Empty");

        if (this.Clients is null || this.Clients.Count is 0)
            throw new NimbusException("No Clients Found");

        string[] ClientList = (from x in this.Clients select x.Name).ToArray();
        this.ExecuteAsync(Encoding.UTF8.GetBytes(Script), ClientList, ClientList.Length);
    }

    public bool IsInjected => this.Clients is not null;

    public List<ClientInfo> Clients
    {
        get
        {
            List<ClientInfo> Clients = [];
            IntPtr ClientPtr = GetClients();

            for (; ; )
            {
                ClientInfo Client = Marshal.PtrToStructure<ClientInfo>(ClientPtr);

                if (Client.Name is null)
                    break;

                Clients.Add(Client);
                ClientPtr += Marshal.SizeOf<ClientInfo>();
            }

            return Clients;
        }
    }

    public void Dispose()
    {
        if (this.Clients is null)
            return;

        this.Clients.Clear();

        IntPtr hModule = GetModuleHandle(this.ModuleName);
        if (hModule != IntPtr.Zero)
        {
            FreeLibrary(hModule);
            hModule = IntPtr.Zero;
        }
    }
}
