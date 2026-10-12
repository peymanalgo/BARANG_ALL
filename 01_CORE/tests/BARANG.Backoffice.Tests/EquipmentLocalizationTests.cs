using System.Globalization;
using System.Resources;
using System.Xml.Linq;
using BARANG.Backoffice.Localization;

namespace BARANG.Backoffice.Tests;

public sealed class EquipmentLocalizationTests
{
    [Fact]
    public void Equipment_captions_exist_in_all_supported_languages()
    {
        string[] keys = ["Master_equipment","GeneralInformation","TechnicalSpecifications","AssignmentLocation","AcquisitionWarranty","FailureHistory","RepairHistory","ServiceMaintenance","InspectionCalibration","DocumentsAttachments","Lifecycle","EquipmentNumber","EquipmentCategory","BrandModel","SerialNumber","EquipmentStatus","Resolve","Complete","Activate","Deactivate","Delete","InService","OutOfService","UnderRepair","UnderMaintenance","Reserved","RetiredDisposed","NextServiceDate","NextDueDate","Passed","Failed"];
        ResourceManager resources = new("BARANG.Backoffice.Localization.AppStrings", typeof(AppStrings).Assembly);
        foreach (string culture in new[] { "fa", "ar", "en" })
            foreach (string key in keys)
                Assert.False(string.IsNullOrWhiteSpace(resources.GetString(key, CultureInfo.GetCultureInfo(culture))), $"Missing Equipment resource {key} for {culture}");
    }

    [Fact]
    public void All_backoffice_localization_families_have_matching_unique_fa_ar_en_keys()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "01_CORE"))) root = root.Parent;
        Assert.NotNull(root);
        string directory = Path.Combine(root!.FullName, "01_CORE", "src", "BARANG.Backoffice", "Localization");
        foreach (string baseFile in Directory.EnumerateFiles(directory, "*.resx").Where(x => !x.EndsWith(".en.resx", StringComparison.OrdinalIgnoreCase) && !x.EndsWith(".ar.resx", StringComparison.OrdinalIgnoreCase) && !x.EndsWith(".fa.resx", StringComparison.OrdinalIgnoreCase)))
        {
            string family = Path.GetFileNameWithoutExtension(baseFile);
            string faFile = File.Exists(Path.Combine(directory, family + ".fa.resx")) ? Path.Combine(directory, family + ".fa.resx") : baseFile;
            string enFile = File.Exists(Path.Combine(directory, family + ".en.resx")) ? Path.Combine(directory, family + ".en.resx") : baseFile;
            string arFile = Path.Combine(directory, family + ".ar.resx");
            Assert.True(File.Exists(arFile), $"Missing ar resource file for {family}.");
            string[] keys = Keys(enFile);
            foreach (string file in new[] { faFile, enFile, arFile }) Assert.Equal(keys.Length, Keys(file).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(keys.Order(StringComparer.Ordinal), Keys(faFile).Order(StringComparer.Ordinal));
            Assert.Equal(keys.Order(StringComparer.Ordinal), Keys(arFile).Order(StringComparer.Ordinal));
        }
    }

    private static string[] Keys(string file) => XDocument.Load(file).Root!.Elements("data").Select(x => x.Attribute("name")!.Value).ToArray();
}
