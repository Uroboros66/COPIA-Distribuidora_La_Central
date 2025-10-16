using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.IO;

public class FacturaPdf : IDocument
{
    private readonly string _texto;
    private readonly byte[] _logo;

    public FacturaPdf(string texto, byte[] logo)
    {
        _texto = texto;
        _logo = logo;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(20);

            page.Header().Height(50).Row(row =>
            {
                row.ConstantItem(50).Image(_logo, ImageScaling.FitArea);
                row.RelativeItem().Text("Factura").FontSize(20).Bold();
            });

            page.Content().Column(col =>
            {
                col.Item().Text($"Contenido: {_texto}").FontSize(14);
                col.Item().PaddingVertical(10).LineHorizontal(1);
                col.Item().Text("¡Gracias por su compra!").Italic();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Distribuidora La Central - ");
                x.Span(DateTime.Now.ToString("dd/MM/yyyy")).SemiBold();
            });
        });
    }
}
