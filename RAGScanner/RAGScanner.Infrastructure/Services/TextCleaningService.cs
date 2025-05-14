using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class TextCleaningService : ITextCleaningService
{
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex OcrArtifactsRegex = new(@"(?<=\w)\s+(?=\w)", RegexOptions.Compiled);
    private static readonly Regex SpecialCharsRegex = new(@"[^\w\s.,!?;:()\-""']", RegexOptions.Compiled);
    private static readonly Regex SingleCharWordsRegex = new(@"\b[a-zA-Z]\b(?!\.)(?=\s|$)", RegexOptions.Compiled);
    private static readonly Regex NumbersInWordsRegex = new(@"(?<=\w)([0-9])(?=\w)", RegexOptions.Compiled);
    private static readonly Regex RepeatedPunctuationRegex = new(@"([.,!?;]){2,}", RegexOptions.Compiled);
    private static readonly Regex MissingSpaceAfterPunctuationRegex = new(@"([.,!?;])(?=[A-Z0-9])", RegexOptions.Compiled);
    private static readonly Regex UnlikelyCharacterCombinationsRegex = new(@"\b\w*[qxjz]{2,}\w*\b", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> CommonOcrErrors = new()
    {
        { "0", "o" },      // Common OCR confusion between 0 and o
        { "1", "l" },      // Common OCR confusion between 1 and l
        { "ll", "ll" },    // Double l is often misrecognized
        { "rn", "m" },     // 'rn' is often misrecognized as 'm'
        { "ii", "n" },     // Double i is often misrecognized as 'n'
        { "vv", "w" },     // Double v is often misrecognized as 'w'
        { "cl", "d" },     // 'cl' is often misrecognized as 'd'
        { "rrn", "rm" },   // 'rrn' is often misrecognized as 'rm'
        { "ni", "m" },     // 'ni' is often misrecognized as 'm'
        { "IVI", "M" },    // 'IVI' is often misrecognized as 'M'
        { "i'", "i" },     // Apostrophe after i is often an artifact
        { "l'", "l" }      // Apostrophe after l is often an artifact
    };

    private static readonly HashSet<string> CommonEnglishWords = new()
    {
        "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
        "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
        "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
        "or", "an", "will", "my", "one", "all", "would", "there", "their", "what"
    };

    public string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Initial cleaning
        text = text.Trim();
        
        // Fix common OCR errors first
        text = FixCommonOcrErrors(text);

        // Remove OCR artifacts and normalize text
        text = OcrArtifactsRegex.Replace(text, string.Empty);
        text = SpecialCharsRegex.Replace(text, " ");
        text = NumbersInWordsRegex.Replace(text, m => MapNumberToLetter(m.Groups[1].Value));
        
        // Fix spacing and punctuation
        text = RepeatedPunctuationRegex.Replace(text, "$1");
        text = MissingSpaceAfterPunctuationRegex.Replace(text, "$1 ");
        
        // Remove unlikely character combinations
        text = UnlikelyCharacterCombinationsRegex.Replace(text, "");
        
        // Remove single character words (except common ones like 'a', 'i')
        text = SingleCharWordsRegex.Replace(text, "");
        
        // Normalize whitespace
        text = MultipleSpacesRegex.Replace(text, " ").Trim();

        // Apply word-based corrections
        text = ApplyWordBasedCorrections(text);

        return text;
    }

    private static string FixCommonOcrErrors(string text)
    {
        return CommonOcrErrors.Aggregate(text, (current, fix) =>
            Regex.Replace(current, fix.Key, fix.Value, RegexOptions.IgnoreCase));
    }

    private static string MapNumberToLetter(string number)
    {
        return number switch
        {
            "0" => "o",
            "1" => "l",
            "3" => "e",
            "4" => "a",
            "5" => "s",
            "6" => "b",
            "7" => "t",
            "8" => "b",
            "9" => "g",
            _ => number
        };
    }

    private string ApplyWordBasedCorrections(string text)
    {
        var words = text.Split(' ');
        var correctedWords = new List<string>();

        foreach (var word in words)
        {
            var correctedWord = word;

            // Skip correction for common English words
            if (CommonEnglishWords.Contains(word.ToLower()))
            {
                correctedWords.Add(word);
                continue;
            }

            // Apply word-specific corrections
            if (word.Length > 2)
            {
                // Fix common word-level OCR errors
                correctedWord = correctedWord.Replace("tbe", "the");
                correctedWord = correctedWord.Replace("tbat", "that");
                correctedWord = correctedWord.Replace("tbe", "the");
                correctedWord = correctedWord.Replace("witb", "with");
                correctedWord = correctedWord.Replace("bave", "have");
            }

            correctedWords.Add(correctedWord);
        }

        return string.Join(" ", correctedWords);
    }
} 