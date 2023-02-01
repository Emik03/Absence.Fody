// SPDX-License-Identifier: MPL-2.0
#if !NETSTANDARD2_0 // ReSharper disable WrongIndentSize
var standardOutput = Func(Console.ReadLine)
   .Forever()
   .Select(Invoke)
   .TakeUntil(string.IsNullOrWhiteSpace);

args
   .DefaultIfEmpty(standardOutput)
   .Where(File.Exists)
   .Select(AssemblyDefinition.ReadAssembly)
   .Filter()
   .Select(x => x.Trim(Console.WriteLine))
   .For(x => x.Write($"Absence.{x.MainModule?.Name}"));
#endif
