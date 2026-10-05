using GeXingzhou.DomainChecks;
var tests = new List<(string Group, string Name, Action Run)>();
ContentChecks.Register(tests);
DialogueMetadataChecks.Register(tests);
MovementChecks.Register(tests);
RestChecks.Register(tests);
StoryChecks.Register(tests);
SoupChecks.Register(tests);
MemoryChecks.Register(tests);
CoinDragChecks.Register(tests);
SaveChecks.Register(tests);
SettingsEventChecks.Register(tests);
string? filter = null;
for (int i = 0; i < args.Length; i++) if (args[i] == "--filter" && i+1 < args.Length) filter = args[++i];
var selected = tests.Where(t => filter is null || t.Group.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
if (selected.Length == 0) { Console.Error.WriteLine("NO_MATCHING_TESTS"); return 2; }
int failed = 0;
foreach (var test in selected) try { test.Run(); Console.WriteLine($"PASS {test.Group}.{test.Name}"); }
catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL {test.Group}.{test.Name}: {ex.Message}"); }
Console.WriteLine($"RESULT {selected.Length-failed}/{selected.Length} passed");
return failed == 0 ? 0 : 1;
