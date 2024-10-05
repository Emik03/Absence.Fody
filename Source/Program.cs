// SPDX-License-Identifier: MPL-2.0
#if !NETSTANDARD2_0
static void OnTrim(IMemberDefinition x) => Console.WriteLine($"Begone, {Join(x)}!");

static string Join(IMemberDefinition x) =>
   x.FindPathToNull(x => x.DeclaringType).Select(x => x.Name).Reverse().Conjoin('.');

static void Dump(Walkies tree)
#if DEBUG
   =>
      tree.Select(Join).Conjoin('\n').Peek(Console.WriteLine);
#else
{ }
#endif
var readLine = Console.ReadLine;

args
   .DefaultIfEmpty(readLine.Forever().Select(Invoke).TakeUntil(string.IsNullOrWhiteSpace))
   .Where(File.Exists)
   .Select(AssemblyDefinition.ReadAssembly)
   .Filter()
   .Lazily(x => new Walkies { x }.Peek(Dump).Trim(x, OnTrim))
   .Lazily(x => x.Write($"Absence.{x.MainModule?.Name}"))
   .Enumerate();
#endif
