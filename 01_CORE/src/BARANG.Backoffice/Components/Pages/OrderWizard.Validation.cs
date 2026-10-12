namespace BARANG.Backoffice.Components.Pages;

public partial class OrderWizard
{
    private void DismissValidation()
    {
        ValidationErrors = [];
        Message = null;
        Success = false;
    }
}
