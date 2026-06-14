using AutoPartsPOS.Application.Sales.Dtos;
using System.Windows.Documents;

namespace AutoPartsPOS.WPF.Sales.Services;

public interface ISalesInvoicePrintService
{
    Task<FlowDocument> CreatePreviewDocumentAsync(SalesInvoiceDetailsDto invoice, CancellationToken cancellationToken = default);

    Task PreviewAsync(SalesInvoiceDetailsDto invoice, CancellationToken cancellationToken = default);

    Task PrintAsync(SalesInvoiceDetailsDto invoice, CancellationToken cancellationToken = default);
}
