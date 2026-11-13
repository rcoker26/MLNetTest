using System.Globalization;
using Microsoft.ML;
using MLNetTest_ConsoleApp;

var usCulture = CultureInfo.GetCultureInfo("en-US");
Console.WriteLine("Walworth County Home Sale Price Prediction");
Console.WriteLine("Warning: This model was trained on residential sales under $1 million.");
Console.WriteLine("Do not rely on these estimates for professional appraisals.");
Console.WriteLine("Enter numeric values using a decimal point (for example, 0.22 or 54500).");
Console.WriteLine();

var modelPath = Path.Combine(AppContext.BaseDirectory, "MLNetTest.mlnet");
if (!File.Exists(modelPath))
{
    Console.Error.WriteLine($"Model file not found: {modelPath}");
    Environment.ExitCode = 1;
    return;
}

try
{
    var context = new MLContext();
    var model = context.Model.Load(modelPath, out _);
    using var engine = context.Model.CreatePredictionEngine<MLNetTest.ModelInput, MLNetTest.ModelOutput>(model);

    while (true)
    {
        var municipality = ReadRequiredText("Municipality: ");
        if (municipality is null) break;
        var propertyType = ReadRequiredText("Property Type (for example, Land and buildings/improvements): ");
        if (propertyType is null) break;
        var acres = ReadNonNegativeNumber("Lot Acres: ");
        if (acres is null) break;

        float landValue;
        float improvementValue;
        float totalAssessedValue;
        while (true)
        {
            var land = ReadNonNegativeNumber("Assessed Land Value: ");
            if (land is null) return;
            var improvements = ReadNonNegativeNumber("Assessed Improvement Value: ");
            if (improvements is null) return;
            landValue = land.Value;
            improvementValue = improvements.Value;
            totalAssessedValue = landValue + improvementValue;
            if (float.IsFinite(totalAssessedValue)) break;
            Console.WriteLine("The combined assessed value is too large. Enter smaller assessed values.");
        }

        var input = new MLNetTest.ModelInput
        {
            Municipality = municipality,
            PropertyType = propertyType,
            // The trained model requires acreage as text, using a decimal point.
            LotAcres = acres.Value.ToString("G9", CultureInfo.InvariantCulture),
            AssessedLandValue = landValue,
            AssessedImprovementValue = improvementValue,
            TotalAssessedValue = totalAssessedValue
        };

        var prediction = engine.Predict(input);
        if (float.IsFinite(prediction.Score))
            Console.WriteLine($"Predicted sale price: {prediction.Score.ToString("C2", usCulture)}");
        else
            Console.WriteLine("The model did not return a finite sale price for this property.");

        Console.WriteLine();
        if (!ReadAnotherPrediction()) break;
        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unable to predict a sale price: {ex.Message}");
    Environment.ExitCode = 1;
}

static string? ReadRequiredText(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var text = Console.ReadLine();
        if (text is null) return null;
        if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
        Console.WriteLine("Please enter a non-empty value.");
    }
}

static float? ReadNonNegativeNumber(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var text = Console.ReadLine();
        if (text is null) return null;
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && float.IsFinite(value) && value >= 0)
            return value;
        Console.WriteLine("Please enter a valid, finite number that is zero or greater.");
    }
}

static bool ReadAnotherPrediction()
{
    while (true)
    {
        Console.Write("Predict another property? (y/n): ");
        var answer = Console.ReadLine()?.Trim();
        if (answer is null || answer.Equals("n", StringComparison.OrdinalIgnoreCase)
            || answer.Equals("no", StringComparison.OrdinalIgnoreCase)) return false;
        if (answer.Equals("y", StringComparison.OrdinalIgnoreCase)
            || answer.Equals("yes", StringComparison.OrdinalIgnoreCase)) return true;
        Console.WriteLine("Please enter y or n.");
    }
}
