using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laboratory_Activity_40
{
    internal class Program
    {
        class Entry
        {
            public string Service { get; }
            public string User { get; }
            public string Pass { get; }

            public Entry(string service, string user, string pass)
            {
                Service = service;
                User = user;
                Pass = pass;
            }
        }

        class Vault
        {
            private const string Header = "VAULTv1";
            private const string SentinelPlain = "CHECK|OK";
            private readonly string filePath;
            private readonly string master;
            private List<Entry> entries; 

            public bool IsUnlocked => entries != null; 

            public Vault(string path, string masterKey)
            {
                filePath = path;
                master = masterKey;
                entries = new List<Entry>();

                if (File.Exists(filePath))
                {
                    LoadVault();
                }
                else
                {
                    CreateNewVault();
                }
            }

            private void LoadVault()
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length == 0 || lines[0] != Header)
                {
                    Console.WriteLine("Invalid vault file.");
                    entries = null; 
                    return;
                }

                if (lines.Length < 2 || Decrypt(lines[1], master) != SentinelPlain)
                {
                    Console.WriteLine("Access denied. Wrong master password.");
                    entries = null;
                    return;
                }

                for (int i = 2; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    string dec = Decrypt(lines[i], master);
                    var parts = dec.Split('|');
                    if (parts.Length == 3)
                        entries.Add(new Entry(parts[0], parts[1], parts[2]));
                }
            }

            private void CreateNewVault()
            {
                File.WriteAllLines(filePath, new[]
                {
            Header,
            Encrypt(SentinelPlain, master)
        });
            }

            public void Add(string service, string user, string pass)
            {
                EnsureUnlocked();
                int idx = entries.FindIndex(e => e.Service.Equals(service, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    entries[idx] = new Entry(service, user, pass);
                else
                    entries.Add(new Entry(service, user, pass));

                Save();
                Console.WriteLine("Saved.");
            }

            public void ListAll()
            {
                EnsureUnlocked();
                foreach (var entry in entries)
                    Console.WriteLine($"{entry.Service}: {Mask(entry.User)}");
            }

            public void Find(string term)
            {
                EnsureUnlocked();
                term = term.ToLower();
                foreach (var entry in entries)
                {
                    if (entry.Service.ToLower().Contains(term))
                        Console.WriteLine($"{entry.Service} -> {entry.User} / {entry.Pass}");
                }
            }

            public void Remove(string service)
            {
                EnsureUnlocked();
                int before = entries.Count;
                entries.RemoveAll(e => e.Service.Equals(service, StringComparison.OrdinalIgnoreCase));
                Save();
                Console.WriteLine(before == entries.Count ? "Not found." : "Removed.");
            }

            private void Save()
            {
                EnsureUnlocked();
                var lines = new List<string> { Header, Encrypt(SentinelPlain, master) };
                foreach (var entry in entries)
                    lines.Add(Encrypt($"{entry.Service}|{entry.User}|{entry.Pass}", master));

                File.WriteAllLines(filePath, lines);
            }

            private void EnsureUnlocked()
            {
                if (!IsUnlocked) throw new InvalidOperationException("Vault is locked.");
            }
            private static string Encrypt(string plain, string key)
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                byte[] k = Encoding.UTF8.GetBytes(key);
                for (int i = 0; i < data.Length; i++) data[i] ^= k[i % k.Length];
                return Convert.ToBase64String(data);
            }

            private static string Decrypt(string b64, string key)
            {
                byte[] data = Convert.FromBase64String(b64);
                byte[] k = Encoding.UTF8.GetBytes(key);
                for (int i = 0; i < data.Length; i++) data[i] ^= k[i % k.Length];
                return Encoding.UTF8.GetString(data);
            }

            private static string Mask(string s)
            {
                if (string.IsNullOrEmpty(s)) return "**";
                if (s.Length <= 2) return new string('*', s.Length);
                return s[0] + new string('*', s.Length - 2) + s[s.Length - 1];
            }
        }

        static void Main()
        {
            Console.Write("Enter master password: ");
            string master = Console.ReadLine() ?? "";

            var vault = new Vault("vault.dat", master);
            if (!vault.IsUnlocked) return;

            while (true)
            {
                Console.Write("\nCommand (add/list/find/remove/exit): ");
                string cmd = (Console.ReadLine() ?? "").Trim().ToLower();

                switch (cmd)
                {
                    case "add":
                        Console.Write("Service: "); var service = Console.ReadLine() ?? "";
                        Console.Write("Username: "); var user = Console.ReadLine() ?? "";
                        Console.Write("Password: "); var pass = Console.ReadLine() ?? "";
                        vault.Add(service, user, pass);
                        break;
                    case "list":
                        vault.ListAll();
                        break;
                    case "find":
                        Console.Write("Search term: "); var term = Console.ReadLine() ?? "";
                        vault.Find(term);
                        break;
                    case "remove":
                        Console.Write("Service to remove: "); var serviceToRemove = Console.ReadLine() ?? "";
                        vault.Remove(serviceToRemove);
                        break;
                    case "exit":
                    case "quit":
                        return;
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }
    }
}














