using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using BARANG.Backoffice.Api;

namespace BARANG.Backoffice.Components.Pages;

public partial class OrderWizard
{
    private bool Step3Viewing;
    private string ArtworkId = "", ArtworkCategory = "", PrintNotes = "", FileMessage = "";
    private List<Option> Artworks = [];
    private IReadOnlyList<SearchableSelectOption> ArtworkOptions => SelectOptions(Artworks);
    private Guid? UploadedFileId;
    private string? UploadedFileVersionId;
    private bool UploadedFilePreviewed;
    private string? UploadedPreviewDataUrl;
    private bool PreviewOpen;
    private string? SelectedDesignPreviewDataUrl;
    private bool DesignPreviewOpen;

    private Task NextFromPrint()
    {
        if (UploadedFileVersionId is not null && !UploadedFilePreviewed)
        {
            ValidationErrors = [L["ArtworkPreviewRequired"]];
            Message = "ValidationError";
            return Task.CompletedTask;
        }
        CurrentStep = 4;
        return Task.CompletedTask;
    }

    private void BackToItems() => CurrentStep = 2;

    private async Task SearchArtworks(string query)
    {
        var session = await Session();
        if (session is null) return;
        string category = string.IsNullOrWhiteSpace(ArtworkCategory) ? "" : $"&categoryCode={Uri.EscapeDataString(ArtworkCategory)}";
        ApiResult<System.Text.Json.JsonElement> result = await Core.GetAsync($"api/v1/artworks/options?search={Uri.EscapeDataString(query)}&limit=30{category}", session.Value.Token, session.Value.Company, default);
        if (result.IsSuccess) Artworks = await ParseOptions(result.Value);
    }

    private async Task SetArtwork(string value)
    {
        ArtworkId = value;
        ArtworkVersionId = "";
        FileVersionId = "";
        UploadedFileId = null;
        UploadedFileVersionId = null;
        await LoadArtworkVersions();
    }

    private async Task LoadArtworkVersions()
    {
        if (!Guid.TryParse(ArtworkId, out Guid artwork)) return;
        var session = await Session();
        if (session is null) return;
        ApiResult<System.Text.Json.JsonElement> result = await Core.GetAsync($"api/v1/artworks/{artwork}/versions", session.Value.Token, session.Value.Company, default);
        if (!result.IsSuccess || result.Value.ValueKind != System.Text.Json.JsonValueKind.Array) return;
        System.Text.Json.JsonElement[] versions = result.Value.EnumerateArray().OrderByDescending(x => N(x, "versionNumber")).ToArray();
        if (versions.Length > 0) ArtworkVersionId = G(versions[0], "id");
    }

    private async Task PreviewSelectedDesign()
    {
        if (!Guid.TryParse(ArtworkId, out Guid artwork)) return;
        var session = await Session();
        if (session is null) return;
        ApiResult<FileDownload> result = await Core.DownloadAsync($"api/v1/artworks/{artwork}/original", session.Value.Token, session.Value.Company, default);
        if (!result.IsSuccess) return;
        SelectedDesignPreviewDataUrl = $"data:{result.Value.ContentType};base64,{Convert.ToBase64String(result.Value.Content)}";
        DesignPreviewOpen = true;
    }

    private void CloseDesignPreview() => DesignPreviewOpen = false;

    private async Task UploadCustomerArtwork(InputFileChangeEventArgs args)
    {
        if (Step3Viewing || args.File is null) return;
        var session = await Session();
        if (session is null) return;
        FileMessage = L["Loading"];
        try
        {
            byte[] previewBytes;
            await using (Stream previewStream = args.File.OpenReadStream(25 * 1024 * 1024))
            {
                using MemoryStream buffer = new();
                await previewStream.CopyToAsync(buffer);
                previewBytes = buffer.ToArray();
            }
            await using Stream stream = new MemoryStream(previewBytes, writable: false);
            ApiResult<System.Text.Json.JsonElement> result = await Core.UploadAsync("api/v1/files/upload", args.File.Name, args.File.ContentType, stream, args.File.Size, session.Value.Token, session.Value.Company, default);
            if (!result.IsSuccess)
            {
                Show(result);
                FileMessage = L["UploadFailed"];
                return;
            }
            UploadedFileId = result.Value.GetProperty("fileId").GetGuid();
            UploadedFileVersionId = result.Value.GetProperty("fileVersionId").GetGuid().ToString();
            FileVersionId = UploadedFileVersionId;
            ArtworkId = ArtworkVersionId = "";
            UploadedFilePreviewed = false;
            UploadedPreviewDataUrl = $"data:{args.File.ContentType};base64,{Convert.ToBase64String(previewBytes)}";
            FileMessage = L["UploadComplete"];
        }
        catch
        {
            FileMessage = L["UploadFailed"];
        }
    }

    private void PreviewUploadedArtwork()
    {
        if (UploadedPreviewDataUrl is null) return;
        UploadedFilePreviewed = true;
        PreviewOpen = true;
    }

    private void CloseArtworkPreview() => PreviewOpen = false;

    private void PickStep3Item(ItemRow row)
    {
        ItemId = row.Id;
        NewStep3Print();
    }

    private void ViewStep3Print(PrintRow row)
    {
        ItemId = row.ItemId;
        PrintId = row.Id;
        PrintSequence = row.Sequence;
        Position = row.Position;
        Method = row.Method;
        PrintWidth = row.Width;
        PrintHeight = row.Height;
        ArtworkVersionId = row.Artwork;
        FileVersionId = row.File;
        PrintNotes = "";
        Step3Viewing = true;
    }

    private void EditStep3Print(PrintRow row)
    {
        ViewStep3Print(row);
        Step3Viewing = false;
    }

    private void EditStep3Print()
    {
        if (PrintId is Guid id && Prints.FirstOrDefault(x => x.Id == id) is PrintRow row)
        {
            EditStep3Print(row);
        }
    }

    private async Task RemoveStep3Print(PrintRow row)
    {
        var session = await Session();
        if (session is null || OrderId is not Guid order) return;
        ApiResult<System.Text.Json.JsonElement> result = await Core.SendAsync(HttpMethod.Post, $"api/v1/orders/{order}/items/{row.ItemId}/print-operations/{row.Id}/remove", new { version = row.Version }, session.Value.Token, session.Value.Company, default);
        Show(result);
        if (result.IsSuccess)
        {
            NewStep3Print();
            await Load();
        }
    }

    private async Task SaveStep3Print()
    {
        ValidationErrors = [];
        if (ItemId is not Guid item)
        {
            Message = "ValidationError";
            ValidationErrors = [L["OrderItemRequired"]];
            return;
        }
        if (PrintWidth is null || PrintWidth <= 0 || PrintHeight is null || PrintHeight <= 0)
        {
            Message = "ValidationError";
            ValidationErrors = [L["PrintDimensionsInvalid"]];
            return;
        }
        if (UploadedFileVersionId is not null && !UploadedFilePreviewed)
        {
            Message = "ValidationError";
            ValidationErrors = [L["ArtworkPreviewRequired"]];
            return;
        }
        var session = await Session();
        if (session is null || OrderId is not Guid order) return;
        var body = new
        {
            sequence = PrintSequence,
            printPositionCode = Position,
            printMethodCode = Null(Method),
            width = PrintWidth,
            height = PrintHeight,
            artworkVersionId = GN(ArtworkVersionId),
            fileVersionId = GN(FileVersionId),
            notes = Null(PrintNotes),
            version = PrintVersion
        };
        ApiResult<System.Text.Json.JsonElement> result = await Core.SendAsync(PrintId is null ? HttpMethod.Post : HttpMethod.Put, $"api/v1/orders/{order}/items/{item}/print-operations" + (PrintId is Guid operationId ? $"/{operationId}" : ""), body, session.Value.Token, session.Value.Company, default);
        Show(result);
        if (!result.IsSuccess) return;
        Guid? uploadedFile = UploadedFileId;
        if (uploadedFile is Guid file && result.Value.TryGetProperty("id", out JsonElement operation))
        {
            ApiResult<System.Text.Json.JsonElement> link = await Core.SendAsync(HttpMethod.Post, $"api/v1/orders/{order}/items/{item}/print-operations/{operation.GetGuid()}/files/{file}/links", new { fileVersionId = GN(FileVersionId), purpose = "Source" }, session.Value.Token, session.Value.Company, default);
            Show(link);
            if (!link.IsSuccess) return;
        }
        NewStep3Print();
        await Load();
    }

    private void NewStep3Print()
    {
        NewPrint();
        ArtworkId = ArtworkCategory = PrintNotes = FileMessage = "";
        UploadedFileId = null;
        UploadedFileVersionId = null;
        UploadedFilePreviewed = false;
        UploadedPreviewDataUrl = null;
        PreviewOpen = false;
        SelectedDesignPreviewDataUrl = null;
        DesignPreviewOpen = false;
        Step3Viewing = false;
    }

    private string PrintPositionLabel(string code) => code switch
    {
        "FRONT" => L["PrintFront"],
        "BACK" => L["PrintBack"],
        "SLEEVE" => L["PrintSleeve"],
        _ => L["PrintOther"]
    };
}
