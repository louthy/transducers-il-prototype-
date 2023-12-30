
using System.Diagnostics;
using LanguageExt;

var slice1 = Slice<char>.Empty;
var slice2 = slice1.Add('a');
var slice3 = slice2.Add('b');


/*var i1 = Transducer.identity<int>();
var i2 = Transducer.identity<int>();
var i3 = Transducer.compose(i1, i2);
var ir = i3.Invoke("no state", 1000, (s, x) => "final");*/

var s1 = Transducer.map((int x) => x.ToString());
var sr = s1.Invoke("no state ", 1000, (s, x) => s + x);

var f1 = (int x) => x.ToString();

var t1 = Transducer.map((int x) => x.ToString());
var t2 = Transducer.map((string x) => x.Length);
var t3 = Transducer.compose(t1, t2);
var t4 = Transducer.compose(t3, t1);
var t5 = Transducer.compose(t4, t2);

var sw = Stopwatch.StartNew();
string r1 =""; 
const int count = 3000000;
foreach (var x in Enumerable.Range(0, count))
{
    r1 = t5.Invoke("no state", 1000, (s, x) => $"result is 1");  // $"result is {x}"
}
sw.Stop();
Console.WriteLine($"{r1}");
Console.WriteLine($"It took {sw.Elapsed.TotalSeconds:F2} seconds for {count:N} iterations.");
Console.WriteLine($"\t{sw.Elapsed.TotalMilliseconds / count:F4} milliseconds per iteration, or");
Console.WriteLine($"\t{sw.Elapsed.TotalNanoseconds / count:F2} nanoseconds per iteration");
