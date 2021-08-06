using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

namespace sql2fs
{
    class Program
    {
        
        static int Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
     
            var scriptingoptions = new ScriptingOptions
            {
                NoCommandTerminator = false,
                AnsiFile = true,
                AppendToFile = false
            };

            var rootCommand = new RootCommand
            {
                new Option<string>(
                    new[] { "--server", "-s" },
                    description: "SQL Server conection (IP address, named instance, machine name)"),
                new Option<string>(
                    new[] { "--directory", "-dir" },
                    description: "Directory to store the files"),
                new Option<string>(
                    new[] { "--database", "-d" },
                    description: "Specific database to connect to, otherwise first database"),
                new Option<string>(
                    new[] { "--types", "-t" },
                    getDefaultValue: () => "all",
                    description: "Types of documentation to include (all, t = table, s = stored procedure, v = view, u = user-defined function)"),
                new Option<bool>(
                    new[] { "--clean", "-c" },
                    getDefaultValue: () => false,
                    description: "Clean the provided directory before saving documentation"),
                new Option<bool>(
                    new[] { "--prune", "-p" },
                    getDefaultValue: () => true,
                    description: "Remove any existing file if the object doesn't exist"),
                new Option<bool>(
                    new[] { "--ignore-encryption", "-e" },
                    getDefaultValue: () => true,
                    description: "Ignore any encrypted object"),
                new Option<string>(
                    new[] { "--name-include", "-ni" },
                    description: "Comma-seperated list of object name prefixes to include"),
                new Option<string>(
                    new[] { "--name-exclude", "-ne" },
                    description: "Comma-seperated list of object name prefixes to exclude"),
                new Option<string>(
                    new[] { "--schema-include", "-si" },
                    description: "Comma-seperated list of object schemas to include"),
                new Option<string>(
                    new[] { "--schema-exclude", "-se" },
                    description: "Comma-seperated list of object schemas to exclude"),
            };
            rootCommand.Description = "A tool for creating a file-system representation of a SQL Server database.";

            rootCommand.Handler = CommandHandler.Create<string, string, string, string, string, string, string, string, bool, bool, bool>
                ((types, directory, server, database, nameinclude, nameexclude, schemainclude, schemaexclude, clean, prune, ignoreencryption) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(server))
                    {
                        Console.WriteLine("A server must be provided.");
                        return;
                    }

                    if (string.IsNullOrEmpty(database))
                    {
                        Console.WriteLine("A database must be provided.");
                        return;
                    }

                    if (string.IsNullOrEmpty(directory))
                    {
                        Console.WriteLine("No directory provided, using current working directory.");
                        directory = Directory.GetCurrentDirectory();
                    }

                    if (!Directory.Exists(directory))
                    {
                        Console.WriteLine("Specified directory does not exist, creating.");
                        Directory.CreateDirectory(directory);
                    }

                    var path = Path.GetFullPath(directory);

                    if (clean)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"DANGER: You are about to clean your source directory {path}.");
                        Console.ResetColor();
                        Console.Write($"Are you sure you'd like to continue [y/N]: ");
                        var key = Console.ReadKey();

                        if (key.Key == ConsoleKey.Y)
                        {
                            Directory.Delete(path, true);
                            Directory.CreateDirectory(path);
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            Console.Write($"Are you sure you'd like to continue [y/N]: N");
                        }
                        Console.WriteLine();
                    }

                    Console.WriteLine($"Connecting to {server}...");
                    var srv = new Server(server);
                    var db = srv.Databases[database];

                    var ninclude = nameinclude.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    var nexclude = nameexclude.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    var sinclude = schemainclude.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    var sexclude = schemaexclude.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToArray();

                    var index = 0;

                    List<(string, string)> objects = new List<(string, string)>();

                    if (types == "all" || types.Contains("t"))
                    {
                        Console.WriteLine($"Documenting tables...");
                        var folder = "Tables";

                        foreach (Table o in db.Tables)
                            if (CheckName(o.Name, o.Schema, ninclude, nexclude, sinclude, sexclude))
                                objects.Add((o.Name, o.Schema));

                        if (!Directory.Exists(Path.Combine(path, folder)) && objects.Count > 0)
                            Directory.CreateDirectory(Path.Combine(path, folder));

                        if (prune && Directory.Exists(Path.Combine(path, folder)))
                        {
                            foreach (var f in Directory.GetFiles(Path.Combine(path, folder)))
                            {
                                if (!objects.Select(s => $"{s.Item2}.{s.Item1}.sql").Any(a => a == Path.GetFileName(f)))
                                {
                                    Console.WriteLine($"Found file {Path.GetFileName(f)} with no object, pruning.");
                                    File.Delete(f);
                                }
                            }
                        }

                        foreach (var obj in objects)
                        {
                            index++;
                            var o = db.Tables[obj.Item1, obj.Item2];

                            Console.WriteLine($"[{index} of {objects.Count}] {o.Schema}.{o.Name}");

                            scriptingoptions.FileName = Path.Combine(path, folder, $"{o.Schema}.{o.Name}.sql");

                            o.Script(scriptingoptions);

                            foreach (ForeignKey fk in o.ForeignKeys)
                                fk.Script(scriptingoptions);

                            foreach (Microsoft.SqlServer.Management.Smo.Index i in o.Indexes)
                                i.Script(scriptingoptions);

                            foreach (Trigger tr in o.Triggers)
                                tr.Script(scriptingoptions);
                        }
                    }

                    objects.Clear();
                    index = 0;

                    if (types == "all" || types.Contains("v"))
                    {
                        Console.WriteLine($"Documenting views...");
                        var folder = "Views";

                        foreach (View o in db.Views)
                            if (CheckName(o.Name, o.Schema, ninclude, nexclude, sinclude, sexclude))
                                objects.Add((o.Name, o.Schema));

                        if (!Directory.Exists(Path.Combine(path, folder)) && objects.Count > 0)
                            Directory.CreateDirectory(Path.Combine(path, folder));

                        if (prune && Directory.Exists(Path.Combine(path, folder)))
                        {
                            foreach (var f in Directory.GetFiles(Path.Combine(path, folder)))
                            {
                                if (!objects.Select(s => $"{s.Item2}.{s.Item1}.sql").Any(a => a == Path.GetFileName(f)))
                                {
                                    Console.WriteLine($"Found file {Path.GetFileName(f)} with no object, pruning.");
                                    File.Delete(f);
                                }
                            }
                        }

                        foreach (var obj in objects)
                        {
                            index++;
                            var o = db.Views[obj.Item1, obj.Item2];

                            Console.WriteLine($"[{index} of {objects.Count}] {o.Schema}.{o.Name}");

                            if (ignoreencryption && o.IsEncrypted)
                                continue;

                            scriptingoptions.FileName = Path.Combine(path, folder, $"{o.Schema}.{o.Name}.sql");
                            
                            o.Script(scriptingoptions);

                            foreach (Microsoft.SqlServer.Management.Smo.Index i in o.Indexes)
                                i.Script(scriptingoptions);

                            foreach (Trigger tr in o.Triggers)
                                tr.Script(scriptingoptions);
                        }
                    }

                    objects.Clear();
                    index = 0;

                    if (types == "all" || types.Contains("s"))
                    {
                        Console.WriteLine($"Documenting stored procedures...");
                        var folder = "Stored Procedures";

                        foreach (StoredProcedure o in db.StoredProcedures)
                            if (CheckName(o.Name, o.Schema, ninclude, nexclude, sinclude, sexclude))
                                objects.Add((o.Name, o.Schema));

                        if (!Directory.Exists(Path.Combine(path, folder)) && objects.Count > 0)
                            Directory.CreateDirectory(Path.Combine(path, folder));

                        if (prune && Directory.Exists(Path.Combine(path, folder)))
                        {
                            foreach (var f in Directory.GetFiles(Path.Combine(path, folder)))
                            {
                                if (!objects.Select(s => $"{s.Item2}.{s.Item1}.sql").Any(a => a == Path.GetFileName(f)))
                                {
                                    Console.WriteLine($"Found file {Path.GetFileName(f)} with no object, pruning.");
                                    File.Delete(f);
                                }
                            }
                        }

                        foreach (var obj in objects)
                        {
                            index++;
                            var o = db.StoredProcedures[obj.Item1, obj.Item2];

                            Console.WriteLine($"[{index} of {objects.Count}] {o.Schema}.{o.Name}");

                            if (ignoreencryption && o.IsEncrypted)
                                continue;

                            scriptingoptions.FileName = Path.Combine(path, folder, $"{o.Schema}.{o.Name}.sql");

                            o.Script(scriptingoptions);
                        }
                    }

                    objects.Clear();
                    index = 0;

                    if (types == "all" || types.Contains("u"))
                    {
                        Console.WriteLine($"Documenting user-defined functions...");
                        var folder = "User-Defined Functions";

                        foreach (UserDefinedFunction o in db.UserDefinedFunctions)
                            if (CheckName(o.Name, o.Schema, ninclude, nexclude, sinclude, sexclude))
                                objects.Add((o.Name, o.Schema));

                        if (!Directory.Exists(Path.Combine(path, folder)) && objects.Count > 0)
                            Directory.CreateDirectory(Path.Combine(path, folder));

                        if (prune && Directory.Exists(Path.Combine(path, folder)))
                        {
                            foreach (var f in Directory.GetFiles(Path.Combine(path, folder)))
                            {
                                if (!objects.Select(s => $"{s.Item2}.{s.Item1}.sql").Any(a => a == Path.GetFileName(f)))
                                {
                                    Console.WriteLine($"Found file {Path.GetFileName(f)} with no object, pruning.");
                                    File.Delete(f);
                                }
                            }
                        }

                        foreach (var obj in objects)
                        {
                            index++;
                            var o = db.UserDefinedFunctions[obj.Item1, obj.Item2];

                            Console.WriteLine($"[{index} of {objects.Count}] {o.Schema}.{o.Name}");

                            if (ignoreencryption && o.IsEncrypted)
                                continue;

                            scriptingoptions.FileName = Path.Combine(path, folder, $"{o.Schema}.{o.Name}.sql");

                            o.Script(scriptingoptions);
                        }
                    }
                }
                catch (Exception ex)
                {                    
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.InnerException.Message.ToString());
                    Console.ResetColor();
                    Environment.Exit(1);
                }
            });

            return rootCommand.InvokeAsync(args).Result;
        }

        
        private static bool CheckName(string name, string schema, string[] ninclude, string[] nexclude, string[] sinclude, string[] sexclude)
        {
            bool allow = true;

            if (new[] { "INFORMATION_SCHEMA", "sys" }.Contains(schema))
                return false;

            if (ninclude.Length > 0 && !ninclude.Any(p => name.ToUpper().StartsWith(p.ToUpper())))
                return false;

            if (nexclude.Length > 0 && nexclude.Any(p => name.ToUpper().StartsWith(p.ToUpper())))
                return false;

            if (sinclude.Length > 0 && !sinclude.Any(p => schema.ToUpper().StartsWith(p.ToUpper())))
                return false;

            if (sexclude.Length > 0 && sexclude.Any(p => schema.ToUpper().StartsWith(p.ToUpper())))
                return false;

            return allow;
        }
    }
}
