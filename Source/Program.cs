// SPDX-License-Identifier: MPL-2.0
#if !NETSTANDARD2_0
// ReSharper disable WrongIndentSize
Func(Console.ReadLine)
   .Forever()
   .Select(Invoke)
   .TakeUntil(string.IsNullOrWhiteSpace)
   .Where(File.Exists)
   .Select(AssemblyDefinition.ReadAssembly)
   .Filter()
   .Select(x => x.Trim(Console.WriteLine))
   .For(x => x.Write($"Absence.{x.MainModule?.Name}"));
#endif
