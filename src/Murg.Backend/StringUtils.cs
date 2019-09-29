using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;
using MutableStringHashSet = System.Collections.Generic.HashSet<string>;

namespace Murg.Backend
{
    public static class StringUtils
    {
        public static List<char> GetCommonSymbols()
        {
            var digits = Range('0', 10).Map(x => (char) x).ToArray();
            var engAlphabet = Range('a', 26)
                .Map(x => (char) x)
                .Bind(x => new[] {x, char.ToUpper(x)});
            return digits.Concat(engAlphabet).Concat(new[] { ' ', '.', ',', '!', '?', '\'', '(', ')', '[', ']' }).ToList();
        }

        public static int MeasureStringCanonicity(string s)
        {
            // todo: fix symbols value
            var res = 0;
            
            const int badLowerCaseValue = -1;
            const int badUpperCaseValue = -3;
            res += s.Split(' ').Filter(w => w.All(char.IsLower)).Count() * badLowerCaseValue;
            res += s.Split(' ').Filter(w => w.All(char.IsUpper)).Count() * badUpperCaseValue;
            
            var isEngLetter = fun<char, bool>(x  => ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z'));
            
            var isBadChar = fun<char, bool>(x => !isEngLetter(x) && !char.IsDigit(x) && !char.IsPunctuation(x) && x != ' ');
            const int badCharValue = -100;
            var getBadCharValue = fun<char, int>(x => !isBadChar(x) ? 0 : badCharValue);
            
            var commonPunctuation = new[] { '.', ',', '!', '?', '(', ')' };
            const int commonPunctuationValue = -1;
            const int uncommonPunctuationValue = -5;
            var getPunctuationCharValue = fun<char, int>(x =>
                !char.IsPunctuation(x) ? 0 :
                commonPunctuation.Contains(x) ? commonPunctuationValue : uncommonPunctuationValue
            );

            var getCharValue = fun<char, int>(x => getBadCharValue(x) + getPunctuationCharValue(x));
            res += s.Map(getCharValue).Sum();

            return res;
        }

        public static int MeasureStringExtravagance(string str)
        {
            var commonSymbols = GetCommonSymbols();
            return str.Length - str.Count(commonSymbols.Contains);
        }

        // TODO: modify transitions costs
        public static int CalculateLevenshteinDistance(string a, string b)
        {
            var al = a.Length;
            var bl = b.Length;
            
            var m = new int[al + 2, bl + 2];
            for (var i = 0; i <= al; i++)
            for (var j = 0; j <= bl; j++)
            {
                m[i, j] = int.MaxValue;
            }
            m[0, 0] = 0;
            
            void Up(int i, int j, int v) => m[i, j] = Math.Min(m[i, j], v);
            
            for (var i = 0; i <= al; i++)
            for (var j = 0; j <= bl; j++)
            {
                Up(i + 1, j, m[i, j] + 1);
                Up(i, j + 1, m[i, j] + 1);
                if (i != al && j != bl) Up(i + 1, j + 1, m[i, j] + (a[i] == b[j] ? 0 : 1));
            }

            return m[al, bl];
        }

        static List<string> GetCharGroupsBy(string str, Func<char, bool> isCharInGroup)
        {
            var res = new List<string>();
            var groupStartIdx = -1;

            void TryDumpGroup(int idx)
            {
                if (groupStartIdx == -1) return;
                res.Add(str.Substring(groupStartIdx, idx - groupStartIdx));
                groupStartIdx = -1;
            }

            for (var i = 0; i < str.Length; i++)
            {
                var isInGroup = isCharInGroup(str[i]);
                if (isInGroup && groupStartIdx == -1) groupStartIdx = i;
                else if (!isInGroup) TryDumpGroup(i);
            }

            TryDumpGroup(str.Length);
            return res;
        }


        public static List<string> SplitToTokens(string str) =>
            GetCharGroupsBy(str, char.IsLetterOrDigit).Map(x => x.ToLower()).ToList();
        
        // [ "my track 1", "my track 2", "track 3" ] -> [ "track" ]
        public static MutableStringHashSet FindCommonTokens(IEnumerable<string> strings)
        {
            var tokens = strings.Map(SplitToTokens).ToList();
            var commonTokens = new MutableStringHashSet(tokens.Count == 0 ? Enumerable.Empty<string>() : tokens[0]);
            tokens.Skip(1).Iter(commonTokens.IntersectWith);
            return commonTokens;
        }

        public static List<string> ExtractDigitGroups(string str) =>
            GetCharGroupsBy(str, char.IsDigit);
        
        // [
        //   "Maaya Sakamoto - Shippo no Uta Single - 01 - Shippo no Uta",
        //   "Maaya Sakamoto - Shippo no Uta Single - 02 - Midori no Hane"
        // ] -> "Maaya Sakamoto - Shippo no Uta Single - "
        public static Func<IEnumerable<string>, string> FindLongestCommonPrefix(bool ignoreCase) => strings =>
        {
            bool Equals(char a, char b) =>
                ignoreCase ? char.ToLower(a) == char.ToLower(b) : a == b;
            string Lcp(string a, string b) =>
                a.Substring(0, a.Zip(b, Equals).TakeWhile(x => x).Count());
            return strings.Reduce(Lcp);
        };
    }
}