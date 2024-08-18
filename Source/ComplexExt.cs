using System;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Celeste.Mod.LocksmithHelper;

// Because I don't like the one that comes with C#
// A Regex is slower but this isn't done in performance critical code so like. Yeah.
internal static partial class ComplexExt {
	private static readonly Regex regex = CompiledRegex();
	
	public static Complex Parse(string input) {
		var real = 0;
        var imag = 0;
		var match = regex.Match(input);
		if (!match.Success) { throw new FormatException(input); }
        var skippedZero = true;
		foreach (Group matchGroup in match.Groups) {
            if (skippedZero) {
                skippedZero = false;
                continue;
            }
			var span = matchGroup.ValueSpan;
			if (span.Length == 0) continue;
            var isImag = span.EndsWith(new ReadOnlySpan<char>('i'));
            if (isImag) {
				imag = span.TrimEnd('i') switch {
					"" or "+" => 1,
					"-" => -1,
					var other => int.Parse(other)
				};
			} else
                real = int.Parse(span);
		}
		
		return new Complex(real, imag);
	}

	public static string AsString(this Complex self) {
		if (self.Imaginary == 0)
			return self.Real.ToString();
		if (self.Real == 0) {
			if (self.Imaginary == 1)
				return "i";
			if (self.Imaginary == -1)
				return "-i";
			return self.Imaginary.ToString() + "i";
		}
		if (self.Imaginary == -1)
			return $"{self.Real}-i";
		if (self.Imaginary == 1)
			return $"{self.Real}+i";
		if (self.Imaginary < 0)
			return $"{self.Real}{self.Imaginary}i";
		return $"{self.Real}+{self.Imaginary}i";
	}

    [GeneratedRegex("^(?=[i\\d+-])([+-]?(?:\\d+)(?![i\\d]))?([+-]?(?:(?:\\d+))?i)?$")]
    private static partial Regex CompiledRegex();

	

    public static double RealWithView(this System.Numerics.Complex self)
        => LocksmithHelperModule.ImaginaryView ? self.Imaginary : self.Real;
    
    public static double ImaginaryWithView(this System.Numerics.Complex self)
        => LocksmithHelperModule.ImaginaryView ? self.Real : self.Imaginary;
}