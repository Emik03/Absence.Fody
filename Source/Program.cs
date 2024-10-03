// SPDX-License-Identifier: MPL-2.0
#if !NETSTANDARD2_0
var readLine = Console.ReadLine;

var readLineUntilNullOrWhitespace = readLine
   .Forever()
   .Select(Invoke)
   .TakeUntil(string.IsNullOrWhiteSpace);

args
   .DefaultIfEmpty(readLineUntilNullOrWhitespace)
   .Where(File.Exists)
   .Select(AssemblyDefinition.ReadAssembly)
   .Filter()
   .Select(x => x.Trim(Console.WriteLine))
   .Lazily(x => x.Write($"Absence.{x.MainModule?.Name}"))
   .Enumerate();
#endif
